import psycopg2
from psycopg2.extras import RealDictCursor
from flask import g, current_app
import os
import json

def init_db():
    current_app.teardown_appcontext(close_db)

def get_db():
    if 'db' not in g:
        with open(os.path.join(current_app.root_path, '.env'), 'r') as env:
            env = json.load(env)
            g.db = psycopg2.connect(
                host=env['host'],
                database=env['database'],
                user=env['user'],
                password=env['password']
            )
            g.db.cursor_factory = RealDictCursor
    return g.db

def close_db(e=None):
    db = g.pop('db', None)
    if db is not None:
        db.close()
