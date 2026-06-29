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
            .with_exec(
                ["sh", "-c", f"for p in {UNIT_TEST_PROJECTS}; do dotnet test $p || exit 1; done"]
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
            .with_exec(
                ["sh", "-c", f"for p in {INTEGRATION_TEST_PROJECTS}; do dotnet test $p || exit 1; done"]
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

    @function
    async def publish(
        self,
        source: BackendDir,
        registry_address: Annotated[str, Doc("e.g. ttl.sh/imagestudio-api:1h")],
    ) -> str:
        """Build the Api as a runtime image and push it to a registry."""
        published = (
            self._sdk(source)
            .with_exec(
                ["dotnet", "publish", "src/Api/Api.csproj", "-c", "Release", "-o", "/out"]
            )
        )
        runtime = (
            dag.container()
            .from_(DOTNET_RUNTIME)
            .with_directory("/app", published.directory("/out"))
            .with_workdir("/app")
            .with_entrypoint(["dotnet", "Api.dll"])
        )
        return await runtime.publish(registry_address)
