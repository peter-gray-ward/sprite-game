from flask import Flask
import uuid
from datetime import datetime
from flaskr.db import get_db
import os


def create_app():
    app = Flask(__name__, instance_relative_config=True)
    
    SECRET_KEY = os.getenv('SECRET_KEY', 'd7155f54-8cc8-11ef-8bd5-784f43a6850a')

    with app.app_context():
        app.config['SECRET_KEY'] = SECRET_KEY

    from . import auth
    app.register_blueprint(auth.api)

    from . import game
    app.register_blueprint(game.api)

    app.add_url_rule('/', endpoint='index')

    return app

