import sys
sys.stdout.reconfigure(encoding='utf-8')
from playwright.sync_api import sync_playwright
import os

with sync_playwright() as p:
    browser = p.chromium.launch()
    
    page_editor = browser.new_page()
    url_editor = 'file:///' + os.path.abspath('editor.html').replace('\\', '/')
    page_editor.goto(url_editor)
    page_editor.wait_for_timeout(1000)
    
    page_editor.evaluate('''
        document.querySelectorAll('.main-tab-btn').forEach(b => {
            b.style.background = '#0f3460';
            b.classList.remove('active');
        });
        document.querySelectorAll('.tab-content').forEach(c => {
            c.style.display = 'none';
            c.classList.remove('active');
        });
        const btn = document.querySelector('.main-tab-btn[data-target="tab-bg"]');
        if (btn) {
            btn.style.background = '#ff477e';
            btn.classList.add('active');
        }
        const tab = document.getElementById('tab-bg');
        if (tab) {
            tab.style.display = 'block';
            tab.classList.add('active');
        }
    ''')
    page_editor.wait_for_timeout(500)
    page_editor.screenshot(path='editor_bg_tab.png', full_page=True)
    
    page_game = browser.new_page()
    url_game = 'file:///' + os.path.abspath('index.html').replace('\\', '/')
    page_game.goto(url_game)
    page_game.wait_for_timeout(2000)
    page_game.screenshot(path='game_bg_test.png', full_page=True)
    browser.close()
    print('done')
