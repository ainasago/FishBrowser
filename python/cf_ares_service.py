"""
Cloudflare 绕过服务 - 基于 CF-Ares
提供 HTTP API 供 C# 应用调用
"""

from flask import Flask, request, jsonify
from cf_ares import AresClient, CloudflareChallengeFailed, CloudflareSessionExpired
import json
import os
from datetime import datetime

app = Flask(__name__)

# 会话存储目录
SESSION_DIR = "cf_sessions"
os.makedirs(SESSION_DIR, exist_ok=True)

# 活跃的客户端缓存
active_clients = {}

def get_session_file(url):
    """根据 URL 生成会话文件名"""
    from urllib.parse import urlparse
    domain = urlparse(url).netloc.replace(":", "_")
    return os.path.join(SESSION_DIR, f"session_{domain}.json")

@app.route('/health', methods=['GET'])
def health_check():
    """健康检查"""
    return jsonify({
        "status": "ok",
        "service": "CF-Ares Service",
        "version": "1.0.0",
        "timestamp": datetime.now().isoformat()
    })

@app.route('/solve', methods=['POST'])
def solve_challenge():
    """
    解决 Cloudflare 挑战
    
    请求体:
    {
        "url": "https://m.iyf.tv/",
        "proxy": "http://user:pass@host:port",  // 可选
        "headless": true,                        // 可选，默认 true
        "browser_engine": "undetected",          // 可选: "seleniumbase", "undetected", "auto"
        "timeout": 60                            // 可选，默认 60
    }
    
    响应:
    {
        "success": true,
        "cookies": {...},
        "user_agent": "...",
        "session_file": "...",
        "message": "挑战成功"
    }
    """
    try:
        data = request.get_json()
        url = data.get('url')
        
        if not url:
            return jsonify({"success": False, "error": "URL is required"}), 400
        
        # 配置参数
        proxy = data.get('proxy')
        headless = data.get('headless', True)
        browser_engine = data.get('browser_engine', 'undetected')
        timeout = data.get('timeout', 60)
        
        print(f"[{datetime.now()}] 开始解决 Cloudflare 挑战: {url}")
        print(f"  - 浏览器引擎: {browser_engine}")
        print(f"  - 无头模式: {headless}")
        print(f"  - 代理: {proxy or '无'}")
        
        # 创建客户端
        client = AresClient(
            browser_engine=browser_engine,
            headless=headless,
            proxy=proxy,
            timeout=timeout
        )
        
        # 执行挑战
        response = client.solve_challenge(url)
        
        print(f"[{datetime.now()}] 挑战成功! 状态码: {response.status_code}")
        
        # 获取会话信息
        session_info = client.get_session_info(url)
        cookies = session_info.get('cookies', {})
        user_agent = session_info.get('user_agent', '')
        
        # 保存会话
        session_file = get_session_file(url)
        client.save_session(session_file)
        
        print(f"[{datetime.now()}] 会话已保存: {session_file}")
        print(f"  - Cookies: {len(cookies)} 个")
        print(f"  - User-Agent: {user_agent[:50]}...")
        
        # 缓存客户端（可选）
        client_id = f"{url}_{datetime.now().timestamp()}"
        active_clients[client_id] = client
        
        return jsonify({
            "success": True,
            "cookies": cookies,
            "user_agent": user_agent,
            "session_file": session_file,
            "client_id": client_id,
            "status_code": response.status_code,
            "message": "挑战成功"
        })
        
    except CloudflareChallengeFailed as e:
        print(f"[{datetime.now()}] 挑战失败: {e}")
        return jsonify({
            "success": False,
            "error": "Cloudflare 挑战失败",
            "details": str(e)
        }), 500
        
    except Exception as e:
        print(f"[{datetime.now()}] 错误: {e}")
        return jsonify({
            "success": False,
            "error": "服务器错误",
            "details": str(e)
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
        "cookies": {...},
        "user_agent": "...",
        "exists": true
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
                "success": False,
                "exists": False,
                "message": "会话不存在"
            })
        
        # 加载会话
        client = AresClient()
        client.load_session(session_file)
        
        session_info = client.get_session_info(url)
        
        return jsonify({
            "success": True,
            "exists": True,
            "cookies": session_info.get('cookies', {}),
            "user_agent": session_info.get('user_agent', ''),
            "session_file": session_file
        })
        
    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500

@app.route('/verify_session', methods=['POST'])
def verify_session():
    """
    验证会话是否仍然有效
    
    请求体:
    {
        "url": "https://m.iyf.tv/",
        "cookies": {...},
        "user_agent": "..."
    }
    
    响应:
    {
        "success": true,
        "valid": true,
        "message": "会话有效"
    }
    """
    try:
        data = request.get_json()
        url = data.get('url')
        cookies = data.get('cookies')
        user_agent = data.get('user_agent')
        
        if not url:
            return jsonify({"success": False, "error": "URL is required"}), 400
        
        # 创建客户端并设置 cookies
        client = AresClient()
        
        # 手动设置 cookies
        for name, value in cookies.items():
            client.cookies[name] = value
        
        # 尝试访问
        try:
            response = client.get(url)
            
            # 检查是否被 Cloudflare 拦截
            is_valid = response.status_code == 200 and 'cloudflare' not in response.text.lower()
            
            return jsonify({
                "success": True,
                "valid": is_valid,
                "status_code": response.status_code,
                "message": "会话有效" if is_valid else "会话已过期"
            })
            
        except CloudflareSessionExpired:
            return jsonify({
                "success": True,
                "valid": False,
                "message": "会话已过期"
            })
        
    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500

@app.route('/close_client', methods=['POST'])
def close_client():
    """
    关闭客户端，释放资源
    
    请求体:
    {
        "client_id": "..."
    }
    """
    try:
        data = request.get_json()
        client_id = data.get('client_id')
        
        if client_id and client_id in active_clients:
            active_clients[client_id].close()
            del active_clients[client_id]
            return jsonify({"success": True, "message": "客户端已关闭"})
        
        return jsonify({"success": False, "message": "客户端不存在"})
        
    except Exception as e:
        return jsonify({"success": False, "error": str(e)}), 500

if __name__ == '__main__':
    print("=" * 60)
    print("🚀 Cloudflare 绕过服务启动中...")
    print("=" * 60)
    print(f"📁 会话存储目录: {os.path.abspath(SESSION_DIR)}")
    print(f"🌐 服务地址: http://localhost:5000")
    print("=" * 60)
    print("\n可用的 API 端点:")
    print("  GET  /health          - 健康检查")
    print("  POST /solve           - 解决 Cloudflare 挑战")
    print("  POST /get_session     - 获取已保存的会话")
    print("  POST /verify_session  - 验证会话是否有效")
    print("  POST /close_client    - 关闭客户端")
    print("\n" + "=" * 60)
    
    app.run(host='0.0.0.0', port=5000, debug=True)
