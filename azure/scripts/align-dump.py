"""Fits a laptop data dump to the schema of the database it is loaded into.

A developer database drifts: tables and columns get added by hand and never make it
into a migration script. The target in Azure is built only from the migration
scripts, so a plain dump of the laptop fails on the first table or column the
target lacks. This rewrites the dump so that:

- a table the target does not have is skipped;
- a table whose columns differ is copied into a temporary staging table first and
  inserted from there, taking only the columns both sides have, cast to the
  target's types;
- a sequence the target does not have is not reset.

Everything skipped is printed, so the person loading knows what did not arrive.

Usage: python3 align-dump.py <dump.sql> <target columns file> <target sequences file> <output.sql>
The columns file has one "table|column|type" line per column of the target's public
tables; the sequences file has one sequence name per line.
"""
import re
import sys

COPY_PATTERN = re.compile(r"^COPY public\.(\"?[\w]+\"?) \((.*)\) FROM stdin;$")
SETVAL_PATTERN = re.compile(r"^SELECT pg_catalog\.setval\('public\.(\"?[\w]+\"?)'")
END_OF_DATA = "\\."


def unquote(name: str) -> str:
    """Returns an identifier without the double quotes pg_dump adds to some names."""
    return name[1:-1] if name.startswith('"') and name.endswith('"') else name


def read_target_columns(path: str) -> dict:
    """Maps each target table to an ordered dict of column name to type."""
    tables = {}
    with open(path, encoding="utf-8") as columns_file:
        for line in columns_file:
            line = line.rstrip("\n")
            if not line:
                continue
            table, column, column_type = line.split("|", 2)
            tables.setdefault(table, {})[column] = column_type
    return tables


def read_target_sequences(path: str) -> set:
    """Returns the names of the target's public sequences."""
    with open(path, encoding="utf-8") as sequences_file:
        return {line.strip() for line in sequences_file if line.strip()}


def staged_copy(table: str, source_columns: list, target_columns: dict) -> tuple:
    """Builds the statements that load one table through a staging table.

    Returns the lines to emit before the data, the lines to emit after it, and the
    source columns the target does not have.
    """
    staging_table = "staging_" + table
    shared = [column for column in source_columns if column in target_columns]
    dropped = [column for column in source_columns if column not in target_columns]
    quoted_source = ", ".join('"' + column + '"' for column in source_columns)
    before = [
        f'CREATE TEMP TABLE "{staging_table}" ({", ".join(chr(34) + c + chr(34) + " text" for c in source_columns)}) ON COMMIT DROP;',
        f'COPY "{staging_table}" ({quoted_source}) FROM stdin;',
    ]
    insert_columns = ", ".join('"' + column + '"' for column in shared)
    select_columns = ", ".join(f'"{column}"::{target_columns[column]}' for column in shared)
    after = [
        f'INSERT INTO public."{table}" ({insert_columns}) OVERRIDING SYSTEM VALUE SELECT {select_columns} FROM "{staging_table}";',
    ]
    return before, after, dropped


def align(dump_lines: list, target_tables: dict, target_sequences: set) -> tuple:
    """Rewrites the dump lines. Returns the new lines and human-readable notes."""
    output, notes = [], []
    index = 0
    while index < len(dump_lines):
        line = dump_lines[index]
        copy_match = COPY_PATTERN.match(line)
        if copy_match:
            table = unquote(copy_match.group(1))
            source_columns = [unquote(c.strip()) for c in copy_match.group(2).split(",")]
            end_index = dump_lines.index(END_OF_DATA, index + 1)
            data_lines = dump_lines[index + 1:end_index]
            if table not in target_tables:
                notes.append(f"Skipped table {table} ({len(data_lines)} rows): it does not exist in the target.")
            elif all(column in target_tables[table] for column in source_columns):
                output.extend(dump_lines[index:end_index + 1])
            else:
                before, after, dropped = staged_copy(table, source_columns, target_tables[table])
                notes.append(f"Table {table}: left out columns the target does not have: {', '.join(dropped)}.")
                output.extend(before + data_lines + [END_OF_DATA] + after)
            index = end_index + 1
            continue
        setval_match = SETVAL_PATTERN.match(line)
        if setval_match and unquote(setval_match.group(1)) not in target_sequences:
            notes.append(f"Skipped sequence {unquote(setval_match.group(1))}: it does not exist in the target.")
            index += 1
            continue
        output.append(line)
        index += 1
    return output, notes


def main() -> None:
    """Reads the dump and the target description, writes the aligned dump."""
    if len(sys.argv) != 5:
        sys.exit("Usage: align-dump.py <dump.sql> <target columns file> <target sequences file> <output.sql>")
    dump_path, columns_path, sequences_path, output_path = sys.argv[1:]
    target_tables = read_target_columns(columns_path)
    if not target_tables:
        sys.exit("The target has no tables. Run the kubapc pipeline before loading data.")
    with open(dump_path, encoding="utf-8") as dump_file:
        dump_lines = dump_file.read().split("\n")
    aligned_lines, notes = align(dump_lines, target_tables, read_target_sequences(sequences_path))
    with open(output_path, "w", encoding="utf-8", newline="\n") as output_file:
        output_file.write("\n".join(aligned_lines))
    for note in notes:
        print(note)
    print(f"Dump aligned to the target: {len(notes)} adjustment(s).")


if __name__ == "__main__":
    main()
