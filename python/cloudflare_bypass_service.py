"""
Cloudflare 绕过服务 - 基于 undetected-chromedriver
提供 HTTP API 供 C# 应用调用
"""

from flask import Flask, request, jsonify
import undetected_chromedriver as uc
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import json
import os
import time
from datetime import datetime
from urllib.parse import urlparse

app = Flask(__name__)

# 会话存储目录
SESSION_DIR = "cf_sessions"
os.makedirs(SESSION_DIR, exist_ok=True)

# 活跃的浏览器实例
active_drivers = {}

def get_session_file(url):
    """根据 URL 生成会话文件名"""
    domain = urlparse(url).netloc.replace(":", "_").replace(".", "_")
    return os.path.join(SESSION_DIR, f"session_{domain}.json")

def save_cookies(driver, session_file):
    """保存 cookies 到文件"""
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

def load_cookies(driver, session_file):
    """从文件加载 cookies"""
    if not os.path.exists(session_file):
        return None
    
    with open(session_file, 'r', encoding='utf-8') as f:
        session_data = json.load(f)
    
    # 添加 cookies
    for cookie in session_data['cookies']:
        try:
            driver.add_cookie(cookie)
        except Exception as e:
            print(f"添加 cookie 失败: {e}")
    
    return session_data

@app.route('/health', methods=['GET'])
def health_check():
    """健康检查"""
    return jsonify({
        "status": "ok",
        "service": "Cloudflare Bypass Service (undetected-chromedriver)",
        "version": "1.0.0",
        "timestamp": datetime.now().isoformat(),
        "active_drivers": len(active_drivers)
    })

@app.route('/solve', methods=['POST'])
def solve_challenge():
    """
    解决 Cloudflare 挑战
    
    请求体:
    {
        "url": "https://m.iyf.tv/",
        "headless": true,
        "timeout": 60,
        "wait_time": 10
    }
    
    响应:
    {
        "success": true,
        "cookies": [...],
        "user_agent": "...",
        "session_file": "...",
        "driver_id": "...",
        "message": "挑战成功"
    }
    """
    driver = None
    try:
        data = request.get_json()
        url = data.get('url')
        
        if not url:
            return jsonify({"success": False, "error": "URL is required"}), 400
        
        headless = data.get('headless', True)
        timeout = data.get('timeout', 60)
        wait_time = data.get('wait_time', 10)
        
        print(f"\n{'='*60}")
        print(f"[{datetime.now()}] 🚀 开始解决 Cloudflare 挑战")
        print(f"{'='*60}")
        print(f"  URL: {url}")
        print(f"  无头模式: {headless}")
        print(f"  超时时间: {timeout}s")
        print(f"  等待时间: {wait_time}s")
        print(f"{'='*60}\n")
        
        # 配置 Chrome 选项
        options = uc.ChromeOptions()
        
        if headless:
            options.add_argument('--headless=new')
        
        # 其他选项
        options.add_argument('--disable-blink-features=AutomationControlled')
        options.add_argument('--disable-dev-shm-usage')
        options.add_argument('--no-sandbox')
        options.add_argument('--disable-gpu')
        
        print(f"[{datetime.now()}] 🔧 启动 undetected-chromedriver...")
        
        # 创建驱动
        driver = uc.Chrome(options=options, version_main=None)
        
        print(f"[{datetime.now()}] ✅ 浏览器启动成功")
        
        # 使用 CDP 设置移动设备模拟（iPhone）
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
            
            print(f"[{datetime.now()}] ✅ 移动设备模拟已设置 (iPhone 12 Pro)")
        except Exception as e:
            print(f"[{datetime.now()}] ⚠️  移动设备设置失败: {e}")
            print(f"[{datetime.now()}] ℹ️  将使用桌面模式")
        
        # 设置超时
        driver.set_page_load_timeout(timeout)
        
        # 访问 URL
        print(f"[{datetime.now()}] 🌐 访问 URL: {url}")
        driver.get(url)
        
        # 等待页面加载
        print(f"[{datetime.now()}] ⏳ 等待 {wait_time} 秒让 Cloudflare 完成验证...")
        time.sleep(wait_time)
        
        # 检查是否成功
        current_url = driver.current_url
        page_source = driver.page_source
        
        # 简单判断是否通过验证
        is_challenge = 'challenge' in page_source.lower() or 'cloudflare' in page_source.lower()
        
        if is_challenge and 'checking your browser' in page_source.lower():
            print(f"[{datetime.now()}] ⚠️  仍在验证中，再等待 10 秒...")
            time.sleep(10)
            page_source = driver.page_source
        
        # 保存会话
        session_file = get_session_file(url)
        session_data = save_cookies(driver, session_file)
        
        print(f"[{datetime.now()}] 💾 会话已保存: {session_file}")
        print(f"[{datetime.now()}] 📊 Cookies: {len(session_data['cookies'])} 个")
        print(f"[{datetime.now()}] 🔍 User-Agent: {session_data['user_agent'][:50]}...")
        
        # 转换 cookies 为字典格式
        cookies_dict = {cookie['name']: cookie['value'] for cookie in session_data['cookies']}
        
        # 缓存驱动（可选）
        driver_id = f"{urlparse(url).netloc}_{int(time.time())}"
        active_drivers[driver_id] = driver
        
        print(f"[{datetime.now()}] ✅ 挑战完成!")
        print(f"{'='*60}\n")
        
        return jsonify({
            "success": True,
            "cookies": cookies_dict,
            "cookies_list": session_data['cookies'],
            "user_agent": session_data['user_agent'],
            "session_file": session_file,
            "driver_id": driver_id,
            "current_url": current_url,
            "message": "挑战成功"
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
            "error": str(e),
            "traceback": traceback.format_exc()
        }), 500

@app.route('/get_session', methods=['POST'])
def get_session():
    """
    获取已保存的会话
    
    请求体:
    {
        "url": "https://m.iyf.tv/"
    }
    
    响应:
    {
        "success": true,
        "exists": true,
        "cookies": {...},
        "user_agent": "..."
    }
    """
    try:
        data = request.get_json()
        url = data.get('url')
        
        if not url:
            return jsonify({"success": False, "error": "URL is required"}), 400
        
        session_file = get_session_file(url)
        
        if not os.path.exists(session_file):
            return jsonify({
                "success": True,
                "exists": False,
                "message": "会话不存在"
            })
        
        with open(session_file, 'r', encoding='utf-8') as f:
            session_data = json.load(f)
        
        cookies_dict = {cookie['name']: cookie['value'] for cookie in session_data['cookies']}
        
        return jsonify({
            "success": True,
            "exists": True,
            "cookies": cookies_dict,
            "cookies_list": session_data['cookies'],
            "user_agent": session_data['user_agent'],
            "session_file": session_file,
            "timestamp": session_data.get('timestamp')
        })
        
    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500

@app.route('/close_driver', methods=['POST'])
def close_driver():
    """
    关闭浏览器驱动
    
    请求体:
    {
        "driver_id": "..."
    }
    """
    try:
        data = request.get_json()
        driver_id = data.get('driver_id')
        
        if driver_id and driver_id in active_drivers:
            driver = active_drivers[driver_id]
            driver.quit()
            del active_drivers[driver_id]
            return jsonify({"success": True, "message": "驱动已关闭"})
        
        return jsonify({"success": False, "message": "驱动不存在"})
        
    except Exception as e:
        return jsonify({"success": False, "error": str(e)}), 500

@app.route('/close_all', methods=['POST'])
def close_all():
    """关闭所有浏览器驱动"""
    try:
        count = 0
        for driver_id in list(active_drivers.keys()):
            try:
                active_drivers[driver_id].quit()
                del active_drivers[driver_id]
                count += 1
            except:
                pass
        
        return jsonify({
            "success": True,
            "message": f"已关闭 {count} 个驱动"
        })
        
    except Exception as e:
        return jsonify({"success": False, "error": str(e)}), 500

if __name__ == '__main__':
    print("\n" + "="*60)
    print("🚀 Cloudflare 绕过服务启动中...")
    print("="*60)
    print(f"📦 使用引擎: undetected-chromedriver")
    print(f"📁 会话存储目录: {os.path.abspath(SESSION_DIR)}")
    print(f"🌐 服务地址: http://localhost:5000")
    print("="*60)
    print("\n可用的 API 端点:")
    print("  GET  /health          - 健康检查")
    print("  POST /solve           - 解决 Cloudflare 挑战")
    print("  POST /get_session     - 获取已保存的会话")
    print("  POST /close_driver    - 关闭指定驱动")
    print("  POST /close_all       - 关闭所有驱动")
    print("\n" + "="*60 + "\n")
    
    try:
        app.run(host='0.0.0.0', port=5000, debug=False)
    finally:
        # 清理所有驱动
        print("\n正在清理资源...")
        for driver in active_drivers.values():
            try:
                driver.quit()
            except:
                pass
