"""
测试 Cloudflare 绕过服务
"""

import requests
import json
import time

BASE_URL = "http://localhost:5000"

def test_health():
    """测试健康检查"""
    print("\n" + "="*60)
    print("1️⃣  测试健康检查")
    print("="*60)
    
    try:
        response = requests.get(f"{BASE_URL}/health")
        data = response.json()
        
        print(f"✅ 服务状态: {data['status']}")
        print(f"✅ 服务名称: {data['service']}")
        print(f"✅ 版本: {data['version']}")
        print(f"✅ 活跃驱动: {data['active_drivers']}")
        return True
    except Exception as e:
        print(f"❌ 健康检查失败: {e}")
        return False

def test_solve_challenge():
    """测试解决 Cloudflare 挑战"""
    print("\n" + "="*60)
    print("2️⃣  测试解决 Cloudflare 挑战")
    print("="*60)
    
    try:
        request_data = {
            "url": "https://m.iyf.tv/",
            "headless": False,  # 显示浏览器窗口
            "timeout": 60,
            "wait_time": 15
        }
        
        print(f"📤 发送请求: {json.dumps(request_data, indent=2, ensure_ascii=False)}")
        print(f"⏳ 等待验证完成（可能需要 15-30 秒）...")
        
        start_time = time.time()
        response = requests.post(
            f"{BASE_URL}/solve",
            json=request_data,
            timeout=120
        )
        elapsed = time.time() - start_time
        
        data = response.json()
        
        if data.get('success'):
            print(f"\n✅ 挑战成功! 耗时: {elapsed:.1f} 秒")
            print(f"✅ Cookies 数量: {len(data.get('cookies', {}))}")
            print(f"✅ User-Agent: {data.get('user_agent', '')[:50]}...")
            print(f"✅ 会话文件: {data.get('session_file', '')}")
            print(f"✅ Driver ID: {data.get('driver_id', '')}")
            
            print(f"\n📊 Cookies:")
            for name, value in list(data.get('cookies', {}).items())[:5]:
                print(f"  - {name}: {value[:30]}...")
            
            return data
        else:
            print(f"\n❌ 挑战失败: {data.get('error', 'Unknown error')}")
            return None
            
    except Exception as e:
        print(f"❌ 测试失败: {e}")
        import traceback
        traceback.print_exc()
        return None

def test_get_session():
    """测试获取会话"""
    print("\n" + "="*60)
    print("3️⃣  测试获取已保存的会话")
    print("="*60)
    
    try:
        request_data = {
            "url": "https://m.iyf.tv/"
        }
        
        response = requests.post(
            f"{BASE_URL}/get_session",
            json=request_data
        )
        
        data = response.json()
        
        if data.get('exists'):
            print(f"✅ 会话存在")
            print(f"✅ Cookies 数量: {len(data.get('cookies', {}))}")
            print(f"✅ User-Agent: {data.get('user_agent', '')[:50]}...")
            print(f"✅ 时间戳: {data.get('timestamp', '')}")
        else:
            print(f"ℹ️  会话不存在（这是正常的，如果还没有运行过 solve）")
        
        return data
        
    except Exception as e:
        print(f"❌ 测试失败: {e}")
        return None

def test_close_driver(driver_id):
    """测试关闭驱动"""
    print("\n" + "="*60)
    print("4️⃣  测试关闭浏览器驱动")
    print("="*60)
    
    try:
        if not driver_id:
            print("ℹ️  没有 driver_id，跳过")
            return
        
        request_data = {
            "driver_id": driver_id
        }
        
        response = requests.post(
            f"{BASE_URL}/close_driver",
            json=request_data
        )
        
        data = response.json()
        
        if data.get('success'):
            print(f"✅ 驱动已关闭")
        else:
            print(f"ℹ️  {data.get('message', '')}")
        
    except Exception as e:
        print(f"❌ 测试失败: {e}")

def main():
    """主测试流程"""
    print("\n" + "="*60)
    print("🧪 Cloudflare 绕过服务测试")
    print("="*60)
    
    # 1. 健康检查
    if not test_health():
        print("\n❌ 服务未运行，请先启动服务:")
        print("   python cloudflare_bypass_service.py")
        return
    
    # 2. 解决挑战
    result = test_solve_challenge()
    
    driver_id = None
    if result:
        driver_id = result.get('driver_id')
        
        # 等待一下
        print("\n⏳ 等待 5 秒...")
        time.sleep(5)
        
        # 3. 获取会话
        test_get_session()
        
        # 4. 关闭驱动
        test_close_driver(driver_id)
    
    print("\n" + "="*60)
    print("✅ 测试完成!")
    print("="*60)
    
    if result:
        print("\n🎉 所有测试通过!")
        print("\n下一步:")
        print("1. 在 C# 中使用 CloudflareAresService")
        print("2. 查看文档: CLOUDFLARE_SIMPLE_SOLUTION.md")
    else:
        print("\n⚠️  挑战失败，可能的原因:")
        print("1. 网络连接问题")
        print("2. Cloudflare 检测到自动化")
        print("3. IP 被封禁")
        print("\n建议:")
        print("1. 使用 headless: false 查看浏览器行为")
        print("2. 增加 wait_time 到 30 秒")
        print("3. 尝试使用代理")

if __name__ == '__main__':
    main()
