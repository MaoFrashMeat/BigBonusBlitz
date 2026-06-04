import os, sys
sys.stdout.reconfigure(encoding='utf-8')
from playwright.sync_api import sync_playwright

with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page()
    errors = []
    page.on('console', lambda msg: errors.append(f'{msg.type}: {msg.text}') if msg.type in ('error','warning') else None)
    page.on('pageerror', lambda err: errors.append(f'PAGEERROR: {err}'))
    
    url = 'file:///' + os.path.abspath('workflow_editor.html').replace('\\', '/')
    page.goto(url)
    page.wait_for_timeout(300)
    
    # Test 1: タブ切り替えが全て機能するか
    roles = ['ハズレ', 'ベル', 'リプレイ', 'チェリー', 'スイカ', 'チャンス', 'ボーナス']
    for i in range(7):
        # 毎回新たにDOMを取得（renderAllで再構築されるため）
        tabs = page.query_selector_all('.role-tab')
        tabs[i].click()
        page.wait_for_timeout(150)
        badge = page.query_selector('.role-badge')
        result = badge.inner_text() if badge else 'NOT FOUND'
        ok = roles[i] in result
        print(f'Tab {i} ({roles[i]}): {"OK" if ok else "FAIL"} -> {result.strip()}')
    
    # Test 2: Tier1 発動率を入力してリアルタイム更新されるか
    tabs = page.query_selector_all('.role-tab')
    tabs[0].click()  # ハズレ
    page.wait_for_timeout(150)
    rate_input = page.query_selector('.sec-rate-input[data-cat="SERIF"]')
    if rate_input:
        rate_input.click()
        rate_input.fill('50')
        page.wait_for_timeout(150)
        total_lbl = page.query_selector('.total-lbl')
        txt = total_lbl.inner_text() if total_lbl else 'N/A'
        print(f'Tier1 rate=50 -> total_lbl: {txt}')
        none_disp = page.query_selector('.none-display')
        none_txt = none_disp.inner_text() if none_disp else 'N/A'
        print(f'None display (should be 50%): {none_txt}')
    
    # Test 3: Tier2 バリアントを入力してtier2合計がリアルタイム更新されるか
    v_input = page.query_selector('.v-input[data-cat="SERIF"][data-var="A"]')
    if v_input:
        v_input.click()
        v_input.fill('30')
        page.wait_for_timeout(150)
        t2_total = page.query_selector('.tier2-total[data-cat="SERIF"]')
        txt = t2_total.inner_text() if t2_total else 'N/A'
        print(f'Tier2 var A=30 -> tier2-total (SERIF): {txt}')
        t2_none = page.query_selector('.tier2-none-disp[data-cat="SERIF"]')
        none_txt = t2_none.inner_text() if t2_none else 'N/A'
        print(f'Tier2 none (SERIF, should be 70%): {none_txt}')
    
    page.screenshot(path='wf_full_test.png', full_page=True)
    
    if errors:
        print('ERRORS:')
        for e in errors:
            print(e)
    else:
        print('No JS errors!')
    browser.close()
