"""
Cloudflare 绕过服务 - 支持手动干预
"""

from flask import Flask, request, jsonify
import undetected_chromedriver as uc
import json
import os
import time
from datetime import datetime
from urllib.parse import urlparse

app = Flask(__name__)

SESSION_DIR = "cf_sessions"
os.makedirs(SESSION_DIR, exist_ok=True)

active_drivers = {}

def get_session_file(url):
    domain = urlparse(url).netloc.replace(":", "_").replace(".", "_")
    return os.path.join(SESSION_DIR, f"session_{domain}.json")

def save_cookies(driver, session_file):
    cookies = driver.get_cookies()
    user_agent = driver.execute_script("return navigator.userAgent")
    
    session_data = {
        "cookies": cookies,
        "user_agent": user_agent,
        "timestamp": datetime.now().isoformat()
    }
    
    with open(session_file, 'w', encoding='utf-8') as f:
        json.dump(session_data, f, indent=2, ensure_ascii=False)
    
    return session_data

@app.route('/health', methods=['GET'])
def health_check():
    return jsonify({
        "status": "ok",
        "service": "Cloudflare Bypass Service (Manual Mode)",
        "version": "1.0.0",
        "active_drivers": len(active_drivers)
    })

@app.route('/solve_manual', methods=['POST'])
def solve_challenge_manual():
    """
    解决 Cloudflare 挑战 - 支持手动干预
    
    请求体:
    {
        "url": "https://m.iyf.tv/",
        "headless": false,
        "manual_wait": 60  # 等待用户手动点击的时间（秒）
    }
    """
    driver = None
    try:
        data = request.get_json()
        url = data.get('url')
        headless = data.get('headless', False)
        manual_wait = data.get('manual_wait', 60)
        
        print(f"\n{'='*60}")
        print(f"[{datetime.now()}] 🚀 启动浏览器（手动模式）")
        print(f"{'='*60}")
        print(f"  URL: {url}")
        print(f"  手动等待时间: {manual_wait}s")
        print(f"{'='*60}\n")
        
        options = uc.ChromeOptions()
        if headless:
            options.add_argument('--headless=new')
        
        options.add_argument('--disable-blink-features=AutomationControlled')
        options.add_argument('--disable-dev-shm-usage')
        options.add_argument('--no-sandbox')
        options.add_argument('--disable-gpu')
        
        print(f"[{datetime.now()}] 🔧 启动浏览器...")
        driver = uc.Chrome(options=options, version_main=None)
        
        print(f"[{datetime.now()}] ✅ 浏览器启动成功")
        
        # 设置移动设备模拟
        print(f"[{datetime.now()}] 📱 设置移动设备指标...")
        try:
            driver.execute_cdp_cmd("Emulation.setDeviceMetricsOverride", {
                "width": 390,
                "height": 844,
                "deviceScaleFactor": 3,
                "mobile": True,
                "screenWidth": 390,
                "screenHeight": 844,
                "positionX": 0,
                "positionY": 0
            })
            
            driver.execute_cdp_cmd("Emulation.setTouchEmulationEnabled", {
                "enabled": True,
                "configuration": "mobile"
            })
            
            driver.execute_cdp_cmd("Emulation.setUserAgentOverride", {
                "userAgent": "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
                "platform": "iPhone"
            })
            
            print(f"[{datetime.now()}] ✅ 移动设备模拟已设置")
        except Exception as e:
            print(f"[{datetime.now()}] ⚠️  移动设备设置失败: {e}")
        
        driver.set_page_load_timeout(120)
        
        print(f"[{datetime.now()}] 🌐 访问 URL: {url}")
        driver.get(url)
        
        print(f"\n{'='*60}")
        print(f"⏳ 等待 {manual_wait} 秒")
        print(f"💡 如果看到 Cloudflare 验证框，请手动点击")
        print(f"💡 如果自动通过，无需操作")
        print(f"{'='*60}\n")
        
        # 等待用户手动操作或自动完成
        time.sleep(manual_wait)
        
        # 检查页面状态
        current_url = driver.current_url
        page_title = driver.title
        
        print(f"[{datetime.now()}] 📊 当前状态:")
        print(f"  URL: {current_url}")
        print(f"  标题: {page_title}")
        
        # 保存会话
        session_file = get_session_file(url)
        session_data = save_cookies(driver, session_file)
        
        print(f"[{datetime.now()}] 💾 会话已保存: {session_file}")
        print(f"[{datetime.now()}] 📊 Cookies: {len(session_data['cookies'])} 个")
        
        cookies_dict = {cookie['name']: cookie['value'] for cookie in session_data['cookies']}
        
        driver_id = f"{urlparse(url).netloc}_{int(time.time())}"
        active_drivers[driver_id] = driver
        
        print(f"[{datetime.now()}] ✅ 完成!")
        print(f"{'='*60}\n")
        
        return jsonify({
            "success": True,
            "cookies": cookies_dict,
            "cookies_list": session_data['cookies'],
            "user_agent": session_data['user_agent'],
            "session_file": session_file,
            "driver_id": driver_id,
            "current_url": current_url,
            "page_title": page_title,
            "message": "挑战完成（可能需要手动操作）"
        })
        
    except Exception as e:
        print(f"[{datetime.now()}] ❌ 错误: {e}")
        import traceback
        traceback.print_exc()
        
        if driver:
            try:
                driver.quit()
            except:
                pass
        
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500

@app.route('/close_driver', methods=['POST'])
def close_driver():
    try:
        data = request.get_json()
        driver_id = data.get('driver_id')
        
        if driver_id and driver_id in active_drivers:
            active_drivers[driver_id].quit()
            del active_drivers[driver_id]
            return jsonify({"success": True, "message": "驱动已关闭"})
        
        return jsonify({"success": False, "message": "驱动不存在"})
    except Exception as e:
        return jsonify({"success": False, "error": str(e)}), 500

if __name__ == '__main__':
    print("\n" + "="*60)
    print("🚀 Cloudflare 绕过服务启动中（手动模式）...")
    print("="*60)
    print(f"📦 使用引擎: undetected-chromedriver")
    print(f"📁 会话存储目录: {os.path.abspath(SESSION_DIR)}")
    print(f"🌐 服务地址: http://localhost:5001")
    print(f"💡 支持手动干预 Cloudflare 验证")
    print("="*60)
    print("\n可用的 API 端点:")
    print("  GET  /health          - 健康检查")
    print("  POST /solve_manual    - 解决挑战（支持手动）")
    print("  POST /close_driver    - 关闭驱动")
    print("\n" + "="*60 + "\n")
    
    try:
        app.run(host='0.0.0.0', port=5001, debug=False)
    finally:
        print("\n正在清理资源...")
        for driver in active_drivers.values():
            try:
                driver.quit()
            except:
                pass
