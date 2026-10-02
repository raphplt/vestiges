#!/usr/bin/env python3
"""Un seul lanceur de validation par checkout ; verrou libéré par le noyau à la sortie."""
import fcntl
import hashlib
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import uuid


def lock_path(root):
    key = hashlib.sha256(str(root).encode()).hexdigest()[:20]
    return Path(tempfile.gettempdir()) / f"vestiges-validation-v1-{os.getuid()}-{key}.lock"


def inherited(root):
    try:
        if os.environ.get("VESTIGES_VALIDATION_LOCK_ROOT") != str(root):
            return False
        owner = int(os.environ["VESTIGES_VALIDATION_LOCK_OWNER"])
        os.kill(owner, 0)
        with lock_path(root).open() as handle:
            if handle.read().strip() != os.environ["VESTIGES_VALIDATION_LOCK_TOKEN"]:
                return False
            try:
                fcntl.flock(handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
                return False
            except BlockingIOError:
                return True
    except (KeyError, ValueError, OSError):
        return False


def main():
    root = Path.cwd().resolve()
    if sys.argv[1] == "--check":
        return 0 if inherited(root) else 1
    with lock_path(root).open("a+") as handle:
        try:
            fcntl.flock(handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            print(f"Validation déjà en cours dans {root} ; aucun build/import lancé.", file=sys.stderr)
            return 1
        env = os.environ.copy()
        token = uuid.uuid4().hex
        handle.seek(0)
        handle.truncate()
        handle.write(token)
        handle.flush()
        env.update(VESTIGES_VALIDATION_LOCK_ROOT=str(root),
                   VESTIGES_VALIDATION_LOCK_TOKEN=token,
                   VESTIGES_VALIDATION_LOCK_OWNER=str(os.getpid()))
        # Le descripteur reste dans ce processus : les serveurs de build .NET ne doivent pas le retenir.
        return subprocess.call(sys.argv[1:], env=env)


if __name__ == "__main__":
    sys.exit(main())
