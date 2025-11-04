using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Engine;

public class FingerprintManager
{
    /// <summary>
    /// 生成指纹注入脚本
    /// </summary>
    public string GenerateInjectionScript(FingerprintProfile profile)
    {
        var script = $@"() => {{
    // 覆盖 webdriver 检测
    Object.defineProperty(navigator, 'webdriver', {{ get: () => undefined }});
    
    // 覆盖 plugins
    Object.defineProperty(navigator, 'plugins', {{ 
        get: () => [
            {{ name: 'Chrome PDF Plugin', description: 'Portable Document Format' }},
            {{ name: 'Chrome PDF Viewer', description: '' }}
        ] 
    }});
    
    // 覆盖 languages
    Object.defineProperty(navigator, 'languages', {{ 
        get: () => ['{profile.Locale}', '{profile.Locale.Split('-')[0]}'] 
    }});
    
    // 覆盖 userAgent
    Object.defineProperty(navigator, 'userAgent', {{ 
        get: () => '{EscapeJavaScript(profile.UserAgent)}' 
    }});
    
    // 覆盖 platform
    Object.defineProperty(navigator, 'platform', {{ 
        get: () => '{profile.Platform}' 
    }});
    
    // Canvas 指纹伪装
    const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
    HTMLCanvasElement.prototype.toDataURL = function() {{
        if (this.width === 280 && this.height === 60) {{
            return 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABgAAAA8CAYAAAA/T+UzAAAA...';
        }}
        return originalToDataURL.apply(this, arguments);
    }};
    
    // WebGL 指纹伪装
    const originalGetParameter = WebGLRenderingContext.prototype.getParameter;
    WebGLRenderingContext.prototype.getParameter = function(parameter) {{
        if (parameter === 37445) {{
            return 'Intel Inc.';
        }}
        if (parameter === 37446) {{
            return 'Intel Iris OpenGL Engine';
        }}
        return originalGetParameter.apply(this, arguments);
    }};
}}";
        return script;
    }

    private string EscapeJavaScript(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}
