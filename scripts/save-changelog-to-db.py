#!/usr/bin/env python3
"""Lee un JSON de changelog desde un archivo y lo guarda en SQL Server."""

import json
import subprocess
import sys
import os

if len(sys.argv) < 2:
    print("Uso: save-changelog-to-db.py <archivo.json> [project_dir]", file=sys.stderr)
    sys.exit(1)

json_file = sys.argv[1]
project_dir = sys.argv[2] if len(sys.argv) > 2 else os.getcwd()

with open(json_file, 'r') as f:
    data = json.load(f)

date = data['date']
summary = data['generalSummary'].replace("'", "''")
total_commits = data['totalCommits']
total_groups = len(data['groups'])

def run_sql(sql):
    result = subprocess.run(
        ['docker', 'compose', 'exec', '-T', 'sqlserver',
         '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', 'sa',
         '-P', 'YourStrong@Passw0rd', '-d', 'AIcoding', '-C', '-h', '-1', '-Q', sql],
        capture_output=True, text=True, cwd=project_dir
    )
    return result

# Borrar si ya existe (CASCADE borra los grupos)
run_sql(f"DELETE FROM DailyChangeSummaries WHERE Date = '{date}'")

# Insertar resumen del dia
insert_sql = f"""
INSERT INTO DailyChangeSummaries (Date, GeneralSummary, TotalCommits, TotalGroups, CreatedAt)
OUTPUT INSERTED.Id
VALUES ('{date}', N'{summary}', {total_commits}, {total_groups}, GETDATE())
"""

result = run_sql(insert_sql)
if result.returncode != 0:
    print(f'Error SQL: {result.stderr}', file=sys.stderr)
    sys.exit(1)

# Obtener el ID insertado (filtrar líneas no numéricas del output de sqlcmd)
summary_id = None
for line in result.stdout.strip().split('\n'):
    line = line.strip()
    if line.isdigit():
        summary_id = line
        break
if summary_id is None:
    print(f'Error obteniendo ID: {result.stdout}', file=sys.stderr)
    sys.exit(1)

print(f'  DailyChangeSummary ID: {summary_id}')

# Insertar cada grupo
for g in data['groups']:
    title = g['groupTitle'].replace("'", "''")
    gsummary = g['groupSummary'].replace("'", "''")
    tags = ','.join(g['tags']) if isinstance(g['tags'], list) else g['tags']

    # commitsJson puede venir como string o como objeto
    if isinstance(g['commitsJson'], str):
        commits_json = g['commitsJson']
    else:
        commits_json = json.dumps(g['commitsJson'], ensure_ascii=False)
    commits_json = commits_json.replace("'", "''")

    order = g.get('displayOrder', 0)

    insert_group = f"""
    INSERT INTO CommitGroups (DailySummaryId, GroupTitle, GroupSummary, Tags, CommitsJson, DisplayOrder)
    VALUES ({summary_id}, N'{title}', N'{gsummary}', N'{tags}', N'{commits_json}', {order})
    """

    result = run_sql(insert_group)
    if result.returncode != 0:
        print(f'  Error insertando grupo: {result.stderr}', file=sys.stderr)
    else:
        print(f'  Grupo: {g["groupTitle"]}')

print('Listo!')
