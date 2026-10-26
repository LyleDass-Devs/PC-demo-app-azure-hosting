"""Adds the hash of index.html's inline import map to its content security policy.

Publishing a Blazor app writes an inline <script type="importmap"> that maps plain
file names to fingerprinted ones. The policy only allows scripts from files
('self'), so the browser would refuse the map and Blazor would never start. Allowing
this one block by its SHA-256 hash keeps every other inline script blocked.

Usage: python3 allow-import-map.py <path to index.html>
"""
import base64
import hashlib
import re
import sys

IMPORT_MAP_PATTERN = re.compile(r'<script type="importmap">(.*?)</script>', re.DOTALL)
SCRIPT_SOURCE_PREFIX = "script-src 'self'"


def main() -> None:
    """Rewrites index.html in place, or exits with a readable error."""
    if len(sys.argv) != 2:
        sys.exit("Usage: allow-import-map.py <path to index.html>")
    index_path = sys.argv[1]
    with open(index_path, encoding="utf-8", newline="") as index_file:
        html = index_file.read()

    match = IMPORT_MAP_PATTERN.search(html)
    if match is None or not match.group(1).strip():
        print("No import map in index.html; nothing to allow.")
        return
    if SCRIPT_SOURCE_PREFIX not in html:
        sys.exit("index.html has no script-src 'self' in its content security policy.")

    digest = hashlib.sha256(match.group(1).encode("utf-8")).digest()
    hash_source = "'sha256-" + base64.b64encode(digest).decode("ascii") + "'"
    if hash_source in html:
        print("Import map hash already allowed.")
        return
    html = html.replace(SCRIPT_SOURCE_PREFIX, SCRIPT_SOURCE_PREFIX + " " + hash_source, 1)
    with open(index_path, "w", encoding="utf-8", newline="") as index_file:
        index_file.write(html)
    print("Allowed the import map with " + hash_source)


if __name__ == "__main__":
    main()
