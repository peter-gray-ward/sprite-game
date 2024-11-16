from flask import (
    Blueprint, g, redirect, render_template, request, url_for, jsonify, send_file
)
from werkzeug.exceptions import abort

from flaskr.auth import login_required
from flaskr.db import get_db
from flaskr.extensions import (
    as_dict, fetchall_as_dict
)

api = Blueprint('game', __name__)

@api.route('/')
def index():
    data = {}
    if 'user' in g:
        user = as_dict(g.user)
        data['user'] = user
    print(data)
    return render_template('game/index.html', data = data)

