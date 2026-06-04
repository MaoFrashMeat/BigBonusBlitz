import re
with open('editor.html', 'r', encoding='utf-8') as f:
    html = f.read()

ids_in_html = set(re.findall(r'id=\"([^\"]+)\"', html))
js_start = html.find('<script>')
js = html[js_start:]
ids_in_js = set(re.findall(r'getElementById\((?:\"|\')([^\'\"]+)(?:\"|\')\)', js))

missing = ids_in_js - ids_in_html
print('Missing IDs:', missing)
