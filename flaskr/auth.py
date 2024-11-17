import functools

from flask import (
    Blueprint, flash, g, redirect, render_template, 
    request, session, url_for, jsonify
)
import json
from werkzeug.security import check_password_hash, generate_password_hash
from datetime import datetime
from flaskr.db import get_db
import uuid

api = Blueprint('auth', __name__, url_prefix='/auth')

@api.route('/register', methods=('POST',))
def register():
    if request.method == 'POST':
        name = request.form['name']
        password = request.form['password']
        
        with get_db() as db:
            error = None
            
            if not name:
                error = 'name is required.'
            elif not password:
                error = 'Password is required.'
            
            if error is None:
                try:
                    cursor = db.cursor()
                    cursor.execute(
                        '''
                            INSERT INTO "user" (id, name, password) 
                            VALUES (%s, %s, %s)
                        ''',
                        (str(uuid.uuid4()), name, generate_password_hash(password))
                    )
                    db.commit()
                    cursor.close()
                except db.IntegrityError:
                    error = f"User {name} is already registered."
                else:
                    return redirect(url_for("auth.login"))
        
        flash(error)

    return render_template('auth/register.html')

@api.route('/login', methods=('POST',))
def login():
    form = json.loads(request.data.decode("utf-8"))
    name = form['username']
    password = form['password']
    with get_db() as db:
        error = None
        
        cursor = db.cursor()
        cursor.execute(
            '''
                SELECT * 
                FROM "user" 
                WHERE name = %s
            ''', 
            (name,)
        )
        user = cursor.fetchone()

        if user is None:
            return jsonify({ 'status': 'incorrect name' })
        elif not check_password_hash(user['password'], password):
            return jsonify({ 'status': 'incorrect password' })
        
        if error is None:
            session.clear()
            session['user_id'] = user['id']
        
        cursor.close()

        return jsonify({ 'status': 'success' })

    return jsonify({ 'status': 'failure' })
        

def login_required(view):
    print('login_required', view)
    @functools.wraps(view)
    def wrapped_view(**kwargs):
        print(g.user)
        if g.user is None:
            return redirect(url_for('auth.login'))   
        return view(**kwargs)
    return wrapped_view


@api.before_app_request
def load_logged_in_user():
    if request.path.startswith('/static/'):
        return

    user_id = session.get('user_id')
    if user_id is None:
        g.user = None
        print('NO USER IN SESSION')
    else:
        with get_db() as db:
            cursor = db.cursor()
            cursor.execute(
                '''
                    SELECT * 
                    FROM "user" 
                    WHERE id = %s
                ''',
                (user_id,)
            )
            g.user = cursor.fetchone()
            cursor.close()


@api.route('/logout')
def logout():
    session.clear()
    return redirect(url_for('index'))

