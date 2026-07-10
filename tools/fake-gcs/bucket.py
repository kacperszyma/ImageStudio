#!/usr/bin/env python3
"""
Manage the bucket in the local fake-gcs-server container (see docker-compose.yml).

  python bucket.py --new    create the bucket
  python bucket.py          list the bucket's objects

Reads STORAGE_EMULATOR_HOST and GCS_BUCKET_NAME from the environment (see
.env), so it targets the same host/bucket the backend does.
"""
import argparse
import os
import sys

import requests

HOST = os.environ.get("STORAGE_EMULATOR_HOST", "http://localhost:4443")
BUCKET = os.environ.get("GCS_BUCKET_NAME", "imagestudio-generations-dev")


def create_bucket(host: str, bucket: str) -> None:
    resp = requests.post(f"{host}/storage/v1/b", params={"project": "local"}, json={"name": bucket})
    if resp.status_code == 409:
        print(f"bucket {bucket!r} already exists")
        return
    resp.raise_for_status()
    print(f"created bucket {bucket!r}")


def list_objects(host: str, bucket: str) -> None:
    resp = requests.get(f"{host}/storage/v1/b/{bucket}/o")
    if resp.status_code == 404:
        print(f"bucket {bucket!r} does not exist — run with --new first", file=sys.stderr)
        sys.exit(1)
    resp.raise_for_status()

    items = resp.json().get("items", [])
    if not items:
        print(f"bucket {bucket!r} is empty")
        return
    for item in items:
        print(f"{item['name']:<50} {int(item['size']):>10} bytes")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--new", action="store_true", help="create the bucket instead of listing it")
    parser.add_argument("--host", default=HOST, help=f"fake-gcs-server URL (default: {HOST})")
    parser.add_argument("--bucket", default=BUCKET, help=f"bucket name (default: {BUCKET})")
    args = parser.parse_args()

    if args.new:
        create_bucket(args.host, args.bucket)
    else:
        list_objects(args.host, args.bucket)


if __name__ == "__main__":
    main()
