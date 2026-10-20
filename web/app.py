from flask import Flask, render_template, request, session, redirect, url_for, jsonify, abort, send_file
from flask_cors import CORS
from io import BytesIO
import requests
import json
import os
import markdown
from bs4 import BeautifulSoup, NavigableString
import re
from pathlib import Path
from urllib.parse import urlparse
from secrets import token_hex

BASE_DIR = Path(__file__).resolve().parent
app = Flask(__name__, root_path=str(BASE_DIR), template_folder="templates", static_folder="static", static_url_path="")
cors = CORS(app, resources={r"*": {"origins": "*"}})

app.secret_key = os.environ.get('HANDPAD_SECRET_KEY') or token_hex(32)

clipboard = ""
drag_clipboard = ""
is_dragging = False

@app.route("/")
def index():
    return "Welcome to the homepage. Visit /page/<filename> to read a markdown file."

@app.route("/tutorial")
def tutorial():
    return render_template('tutorial.html')

@app.route('/api/tutorial_data', methods=['GET'])
def get_tutorial_data():
    try:
        with open(BASE_DIR / "tutorial_data.json", "r", encoding="utf-8") as f:
            data = json.load(f)
        return jsonify(data)
    except FileNotFoundError:
        return jsonify({"error": "tutorial_data.json not found"}), 404

@app.route('/canvas')
def canvas():
    return render_template('canvas_template.html')

@app.route('/canvas2')
def canvas2():
    return render_template('canvas_collage.html')

@app.route('/map')
def map():
    return render_template('map_template.html')

@app.route('/api/canvas_data', methods=['GET'])
def get_canvas_data():
    try:
        with open(BASE_DIR / "canvas_data.json", "r", encoding="utf-8") as f:
            data = json.load(f)
        return jsonify(data)
    except FileNotFoundError:
        return jsonify({"error": "canvas_data.json not found"}), 404
    
def wrap_in_span(tag):
    if tag.name == 'li':  # Special handling for <li> tags
        inner_content = ''.join(str(content) for content in tag.contents)
        new_content = f'<span>{inner_content}</span>'
        tag.clear()
        tag.append(BeautifulSoup(new_content, 'html.parser'))
        return

    if isinstance(tag.string, NavigableString):  # Check for None and NavigableString
        sentences = re.split(r'(?<=[.!?])\s+', tag.string)
        new_content = ''.join(f'<span>{sentence.strip()}</span> ' for sentence in sentences)
        tag.string.replace_with(BeautifulSoup(new_content, 'html.parser'))
    else:  # Tag contains other tags
        new_content = ""
        for content in tag.contents:
            if content.name is None:  # Means this part is a NavigableString
                sentences = re.split(r'(?<=[.!?])\s+', str(content))
                new_content += ''.join(f'<span>{sentence.strip()}</span> ' for sentence in sentences)
            else:
                new_content += str(content)
        tag.clear()
        tag.append(BeautifulSoup(new_content, 'html.parser'))
    
@app.route("/page/<path:filename>")
def show_page(filename):
    if not filename.endswith(".md"):
        filename += ".md"

    pages_dir = (BASE_DIR / "pages").resolve()
    filepath = (pages_dir / filename).resolve()
    if not filepath.is_relative_to(pages_dir):
        abort(404)

    if not os.path.exists(filepath):
        abort(404)

    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()

    content_html = markdown.markdown(content)
    
    # Process HTML with BeautifulSoup
    soup = BeautifulSoup(content_html, 'html.parser')

    # Target the specific tags you want to modify
    for tag in soup.find_all(['p', 'li', 'h1', 'h2', 'h3']):  # Add more tags as needed
        wrap_in_span(tag)

    content_html = str(soup)

    # titlecase filename and remove .md
    filename = filename[:-3].replace("-", " ").title()

    return render_template("page_template.html", content=content_html, filename=filename)

@app.route('/quiz', methods=['GET', 'POST'])
def quiz():
    item_index = request.args.get('item', default=0, type=int)

    with open(BASE_DIR / "quiz_data.json", "r", encoding="utf-8") as f:
        quiz_data = json.load(f)

    if item_index < 0 or item_index >= len(quiz_data):
        item_index = 0  # Reset to first question if index is out of range

    if request.method == 'POST':
        next_item = item_index + 1
        if next_item >= len(quiz_data):
            return render_template('quiz_template.html', completed=True)
        else:
            return redirect(url_for('quiz', item=next_item))

    current_data = quiz_data[item_index]
    return render_template('quiz_template.html', question_data=current_data, completed=False)


@app.route('/api/clipboard', methods=['GET'])
def get_clipboard():
    global clipboard
    return jsonify({"clipboard": clipboard})


@app.route('/api/clipboard', methods=['POST'])
def set_clipboard():
    global clipboard
    data = request.get_json()
    if data is not None and 'clipboard' in data:
        clipboard = data['clipboard']
        return jsonify({"status": "success", "message": "Clipboard updated successfully"})
    else:
        return jsonify({"status": "error", "message": "Invalid request"}), 400
    
@app.route('/fetch_image')
def fetch_image():
    url = request.args.get('url')
    parsed = urlparse(url or '')
    allowed_hosts = {'upload.wikimedia.org', 'cdn.britannica.com',
                     'media.cntraveler.com', 'png.pngtree.com', 'www.nationalgeographic.com'}
    if parsed.scheme != 'https' or parsed.hostname not in allowed_hosts or parsed.username or parsed.password:
        abort(400)
    try:
        response = requests.get(url, timeout=15, allow_redirects=False)
        if response.status_code != 200:
            abort(502)
    except requests.RequestException:
        abort(502)
    content_type = response.headers.get('Content-Type', '').split(';')[0]
    if not content_type.startswith('image/'):
        abort(502)
    return send_file(BytesIO(response.content), mimetype=content_type)

@app.route('/api/drag_status', methods=['GET'])
def get_drag_status():
    global is_dragging, drag_clipboard
    return jsonify({"is_dragging": is_dragging, "drag_content": drag_clipboard})


@app.route('/api/drag', methods=['POST'])
def set_drag_info():
    global drag_clipboard, is_dragging
    data = request.get_json()
    if data is not None and 'drag_content' in data:
        drag_clipboard = data['drag_content']
        is_dragging = data.get('is_dragging', False)
        return jsonify({"status": "success", "message": "Drag content updated successfully"})
    else:
        return jsonify({"status": "error", "message": "Invalid request"}), 400



if __name__ == "__main__":
    app.run(host='127.0.0.1', port=5000, debug=False)
