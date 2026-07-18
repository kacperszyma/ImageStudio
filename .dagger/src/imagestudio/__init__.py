"""ImageStudio CI/CD pipeline.

The module lives at the repo root, so a function can reach both `backend/` and
`frontend/`. Every step runs in a container — identical on a laptop and in CI —
so there is no "works on my machine".

  dagger call build           # restore + build the backend solution
  dagger call test            # backend unit + integration tests
  dagger call frontend-build  # type-check + production build of the React app
  dagger call ci              # everything CI needs to go green

The directory arguments default to the right subfolder (`--source` → backend/,
`--frontend` → frontend/), so the commands above need no extra flags. Local
*dev mode* is a separate concern: Dagger only runs containers, so use the
Makefile (`make dev`) to run the app natively with hot reload.
"""

from typing import Annotated

import dagger
from dagger import DefaultPath, Doc, Ignore, dag, function, object_type

DOTNET_SDK = "mcr.microsoft.com/dotnet/sdk:10.0"
DOTNET_RUNTIME = "mcr.microsoft.com/dotnet/aspnet:10.0"
POSTGRES = "postgres:16"
NODE = "node:22"
OTEL_COLLECTOR = "otel/opentelemetry-collector-contrib:0.106.0"
FIREBASE_TOOLS_VERSION = "15.24.0"

UNIT_TEST_PROJECTS = (
    "tests/Wallet.UnitTests "
    "tests/Api.UnitTests "
    "tests/Generation.UnitTests "
    "tests/Users.UnitTests"
)
INTEGRATION_TEST_PROJECTS = (
    "tests/Wallet.IntegrationTests "
    "tests/GenerationManager.IntegrationTests"
)

# Build artifacts never need to enter a container — excluding them keeps the
# uploaded context small and avoids host/container framework-version clashes.
BACKEND_IGNORE = ["**/bin", "**/obj"]
FRONTEND_IGNORE = ["node_modules", "dist"]

# A function reaching for backend/ or frontend/ declares it this way; the default
# path is relative to the module root (the repo root) so callers pass no flags.
BackendDir = Annotated[
    dagger.Directory, DefaultPath("/backend"), Ignore(BACKEND_IGNORE), Doc("The backend/ directory")
]
FrontendDir = Annotated[
    dagger.Directory, DefaultPath("/frontend"), Ignore(FRONTEND_IGNORE), Doc("The frontend/ directory")
]


@object_type
class Imagestudio:
    def _sdk(self, source: dagger.Directory) -> dagger.Container:
        """Base .NET SDK container with the source mounted and the NuGet cache shared."""
        return (
            dag.container()
            .from_(DOTNET_SDK)
            # Persist downloaded packages across runs so restore is fast after the first.
            .with_mounted_cache("/root/.nuget/packages", dag.cache_volume("nuget-packages"))
            .with_directory("/src", source)
            .with_workdir("/src")
        )

    def _node(self, frontend: dagger.Directory) -> dagger.Container:
        """Base Node container with the frontend installed and the npm cache shared."""
        return (
            dag.container()
            .from_(NODE)
            .with_mounted_cache("/root/.npm", dag.cache_volume("npm-cache"))
            .with_directory("/app", frontend)
            .with_workdir("/app")
            .with_exec(["npm", "ci"])
        )

    def _postgres(self) -> dagger.Service:
        """A throwaway Postgres the integration tests create/drop their own DBs in.

        Credentials match what the test fixtures expect (postgres/postgres). Dagger
        waits for the exposed port to accept connections before binding it.
        """
        return (
            dag.container()
            .from_(POSTGRES)
            .with_env_variable("POSTGRES_USER", "postgres")
            .with_env_variable("POSTGRES_PASSWORD", "postgres")
            .with_exposed_port(5432)
            # Run the image's normal startup (initdb + apply POSTGRES_* vars).
            # Calling the entrypoint explicitly keeps this working regardless of
            # the SDK's as_service(use_entrypoint=...) availability.
            .with_exec(["docker-entrypoint.sh", "postgres"])
            .as_service()
        )

    @function
    async def build(self, source: BackendDir) -> str:
        """Restore and build the whole backend solution."""
        return await (
            self._sdk(source)
            .with_exec(["dotnet", "build", "ImageStudio.slnx", "-c", "Release"])
            .stdout()
        )

    @function
    async def unit_test(self, source: BackendDir) -> str:
        """Run the unit-test projects (no database needed).

        `dotnet test` takes a single project/solution, so run one per project
        and fail fast — passing several at once is read as MSBuild switches.
        """
        return await (
            self._sdk(source)
            .with_env_variable("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "1")
            .with_exec(
                ["sh", "-c", f'for p in {UNIT_TEST_PROJECTS}; do dotnet test $p --verbosity quiet --logger "console;verbosity=normal" || exit 1; done']
            )
            .stdout()
        )

    @function
    async def integration_test(self, source: BackendDir) -> str:
        """Run the integration tests against a bound Postgres service.

        The fixtures read PGHOST/PGPORT; here Postgres is reachable at host 'db'.
        Includes the saga partial-failure tests.
        """
        return await (
            self._sdk(source)
            .with_service_binding("db", self._postgres())
            .with_env_variable("PGHOST", "db")
            .with_env_variable("PGPORT", "5432")
            .with_env_variable("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "1")
            .with_exec(
                ["sh", "-c", f'for p in {INTEGRATION_TEST_PROJECTS}; do dotnet test $p --verbosity quiet --logger "console;verbosity=normal" || exit 1; done']
            )
            .stdout()
        )

    @function
    async def test(self, source: BackendDir) -> str:
        """Run backend unit tests, then integration tests."""
        out = await self.unit_test(source)
        out += "\n" + await self.integration_test(source)
        return out

    @function
    async def frontend_build(self, frontend: FrontendDir) -> str:
        """Type-check and produce a production build of the React app."""
        return await (
            self._node(frontend)
            .with_exec(["npm", "run", "build"])
            .stdout()
        )

    @function
    async def frontend_lint(self, frontend: FrontendDir) -> str:
        """Lint the React app."""
        return await (
            self._node(frontend)
            .with_exec(["npm", "run", "lint"])
            .stdout()
        )

    @function
    async def ci(self, source: BackendDir, frontend: FrontendDir) -> str:
        """Everything CI runs: backend build + tests and frontend lint + build."""
        out = await self.build(source)
        out += "\n" + await self.test(source)
        out += "\n" + await self.frontend_lint(frontend)
        out += "\n" + await self.frontend_build(frontend)
        return out

    def _runtime_image(self, source: dagger.Directory) -> dagger.Container:
        """The Api's runtime image: publish output copied onto the ASP.NET runtime base."""
        published = self._sdk(source).with_exec(
            ["dotnet", "publish", "src/Api/Api.csproj", "-c", "Release", "-o", "/out"]
        )
        return (
            dag.container()
            .from_(DOTNET_RUNTIME)
            .with_directory("/app", published.directory("/out"))
            .with_workdir("/app")
            .with_entrypoint(["dotnet", "Api.dll"])
        )

    @function
    async def publish(
        self,
        source: BackendDir,
        registry_address: Annotated[str, Doc("e.g. ttl.sh/imagestudio-api:1h")],
    ) -> str:
        """Build the Api as a runtime image and push it to a registry."""
        return await self._runtime_image(source).publish(registry_address)

    @function
    async def deploy_backend(
        self,
        source: BackendDir,
        registry_address: Annotated[
            str, Doc("e.g. us-central1-docker.pkg.dev/PROJECT_ID/imagestudio/api:GIT_SHA")
        ],
        gcp_token: Annotated[
            dagger.Secret,
            Doc(
                "A GCP OAuth2 access token — run `gcloud auth login` then pass "
                "--gcp-token=cmd:'gcloud auth print-access-token'. No service-account "
                "key file involved; this always forces a human, browser-based login."
            ),
        ],
    ) -> str:
        """Build the Api runtime image and push it to Google Artifact Registry.

        Artifact Registry accepts a short-lived OAuth2 access token as the
        docker-login password with the literal username `oauth2accesstoken` —
        that's the identity `gcloud auth login` just put in your browser.
        """
        runtime = self._runtime_image(source).with_registry_auth(
            registry_address, "oauth2accesstoken", gcp_token
        )
        return await runtime.publish(registry_address)

    @function
    async def deploy_frontend(
        self,
        frontend: FrontendDir,
        project_id: Annotated[str, Doc("Firebase project ID, e.g. pebbleimage-e8167")],
        gcp_service_account_key: Annotated[
            dagger.Secret,
            Doc(
                "JSON key for a service account with roles/firebasehosting.admin on the "
                "Firebase project. Mounted as a file — the Firebase CLI picks it up via "
                "GOOGLE_APPLICATION_CREDENTIALS, no `firebase login` involved."
            ),
        ],
    ) -> str:
        """Build the React app and deploy dist/ to Firebase Hosting (see frontend/firebase.json)."""
        return await (
            self._node(frontend)
            .with_exec(["npm", "run", "build"])
            .with_mounted_secret("/run/secrets/gcp-key.json", gcp_service_account_key)
            .with_env_variable("GOOGLE_APPLICATION_CREDENTIALS", "/run/secrets/gcp-key.json")
            .with_exec(
                [
                    "npx", "--yes", f"firebase-tools@{FIREBASE_TOOLS_VERSION}",
                    "deploy", "--only", "hosting", "--project", project_id, "--non-interactive",
                ]
            )
            .stdout()
        )

    @function
    async def publish_otel_sidecar(
        self,
        registry_address: Annotated[
            str, Doc("e.g. us-central1-docker.pkg.dev/PROJECT_ID/imagestudio/otel-collector:GIT_SHA")
        ],
        gcp_token: Annotated[
            dagger.Secret,
            Doc(
                "A GCP OAuth2 access token — run `gcloud auth login` then pass "
                "--gcp-token=cmd:'gcloud auth print-access-token'."
            ),
        ],
        config: Annotated[
            dagger.File,
            DefaultPath("/observability/otel-collector-config.yml"),
            Doc("otel-collector config baked into the image"),
        ],
    ) -> str:
        """Build the otel-collector image with our config baked in and push it to Artifact Registry.

        Cloud Run sidecars share localhost with the ingress container but can't
        mount arbitrary host files, so the config is baked into the image rather
        than passed via a `-config` flag at deploy time. The upstream image's
        default config path is /etc/otelcol-contrib/config.yaml, so overwriting
        that file is all that's needed — no entrypoint change.

        Before deploying, point exporters (tempo:4317, loki:3100/otlp) at
        reachable endpoints — those hostnames only resolve inside the local
        docker-compose network.
        """
        image = (
            dag.container()
            .from_(OTEL_COLLECTOR)
            .with_file("/etc/otelcol-contrib/config.yaml", config)
            .with_registry_auth(registry_address, "oauth2accesstoken", gcp_token)
        )
        return await image.publish(registry_address)
