from flask import (
    Blueprint, g, redirect, 
    render_template, request, 
    url_for, jsonify, send_file,
    session, Response
)
import json
from werkzeug.exceptions import abort
import requests
from flaskr.auth import login_required
from flaskr.db import get_db
from flaskr.extensions import (
    as_dict, fetchall_as_dict
)
import psycopg2
from psycopg2.extras import execute_values
import uuid

api = Blueprint('game', __name__)

@api.route('/')
def index():
    data = {}
    if g.user is not None:
        print(g.user)
        user = as_dict(g.user)
        data['user'] = user
    return render_template('game/index.html', data = data)


@api.route('/save-image', methods=('POST',))
@login_required
def save_image():
    user_id = session.get('user_id')
    try:
        body = json.loads(request.data.decode('utf-8'))

        url = body['url']
        tag = body['tag']
        print(f"Image URL: {url}")

        # Stream the image from the URL
        response = requests.get(url, stream=True)
        response.raise_for_status()  # Raise an error for bad responses (4xx, 5xx)

        # Get the binary content of the image
        image_data = response.content

        # Generate a UUID for the image
        image_id = str(uuid.uuid4())

        # Insert the image into the database
        with get_db() as conn:  # Assuming `get_db()` provides the database connection
            cursor = conn.cursor()
            cursor.execute(
                "INSERT INTO public.images (id, tag, src, user_id) VALUES (%s, %s, %s, %s)",
                (image_id, tag, psycopg2.Binary(image_data), user_id,)
            )
            conn.commit()

            cursor.close()

            return jsonify({"status": "success", "id": str(image_id)}), 201

    except requests.exceptions.RequestException as e:
        print(f"Error fetching image: {e}")
        return jsonify({"status": "error", "message": "Failed to fetch image"}), 400

    except psycopg2.Error as e:
        print(f"Database error: {e}")
        return jsonify({"status": "error", "message": "Failed to save image"}), 500


@api.route('/get-image-ids', methods=('GET',))
def get_image_ids():
    user_id = session.get('user_id')
    try:
        with get_db() as db:
            cursor = db.cursor()
            cursor.execute(
                '''
                    SELECT id, tag
                    FROM public.images
                    WHERE user_id = %s
                    GROUP BY tag, id
                ''',
                (user_id,)
            )
            res = fetchall_as_dict(cursor)
            cursor.close()
            return jsonify({ 'status': 'success', 'data': res })
    except Exception as e:
        return jsonify({ 'status': 'error', 'messsage': f'{e}' })

@api.route('/get-image/<image_id>', methods=('GET',))
@login_required
def get_image(image_id):
    user_id = session.get('user_id')
    try:
        with get_db() as db:
            cursor = db.cursor()
            cursor.execute(
                '''
                    SELECT src
                    FROM public.images
                    WHERE user_id = %s
                    AND id = %s
                ''',
                (user_id, image_id,)
            )
            row = cursor.fetchone()

            cursor.close()

            return Response(row['src'], mimetype='image/png')
    except Exception as e:
        return jsonify({ 'status': 'error', 'messsage': f'{e}' })





