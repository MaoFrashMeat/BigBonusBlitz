import re
with open('settings.js', 'r', encoding='utf-8') as f:
    text = f.read()

text = re.sub(r'\"probabilities_Tier2\":\s*\{[^}]+\},\s*', '', text)

with open('settings.js', 'w', encoding='utf-8') as f:
    f.write(text)
