using System;
using System.Collections.Generic;
using System.Linq;

namespace FishBrowser.WPF.Services
{
    /// <summary>
    /// GPU 数据库服务
    /// 提供大量真实的 WebGL Vendor 和 Renderer 组合
    /// </summary>
    public class GpuDatabase
    {
        private static readonly Random _random = new Random();

        // GPU 配置数据库（Vendor + Renderer 配对）
        private static readonly List<GpuConfig> GpuConfigs = new List<GpuConfig>
        {
            // NVIDIA GPUs
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 4090 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 4080 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 4070 Ti Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 4070 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 4060 Ti Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 4060 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 3090 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 3080 Ti Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 3080 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 3070 Ti Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 3070 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 3060 Ti Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 3060 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce RTX 3050 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce GTX 1660 Ti Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce GTX 1660 SUPER Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (NVIDIA)", "ANGLE (NVIDIA GeForce GTX 1650 Direct3D11 vs_5_0 ps_5_0)", "Windows"),

            // AMD GPUs
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 7900 XTX Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 7900 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 7800 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 7700 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 7600 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6950 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6900 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6800 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6800 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6750 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6700 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6650 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6600 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6600 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 6500 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 5700 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (AMD)", "ANGLE (AMD Radeon RX 5600 XT Direct3D11 vs_5_0 ps_5_0)", "Windows"),

            // Intel GPUs
            new GpuConfig("Google Inc. (Intel)", "ANGLE (Intel(R) Arc A770 Graphics Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (Intel)", "ANGLE (Intel(R) Arc A750 Graphics Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (Intel)", "ANGLE (Intel(R) Arc A380 Graphics Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (Intel)", "ANGLE (Intel(R) UHD Graphics 770 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (Intel)", "ANGLE (Intel(R) UHD Graphics 730 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (Intel)", "ANGLE (Intel(R) UHD Graphics 630 Direct3D11 vs_5_0 ps_5_0)", "Windows"),
            new GpuConfig("Google Inc. (Intel)", "ANGLE (Intel(R) Iris Xe Graphics Direct3D11 vs_5_0 ps_5_0)", "Windows"),

            // macOS GPUs (Metal backend)
            new GpuConfig("Apple Inc.", "Apple M3 Max", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M3 Pro", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M3", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M2 Ultra", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M2 Max", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M2 Pro", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M2", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M1 Ultra", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M1 Max", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M1 Pro", "MacOS"),
            new GpuConfig("Apple Inc.", "Apple M1", "MacOS"),
            new GpuConfig("AMD", "AMD Radeon Pro 5500M", "MacOS"),
            new GpuConfig("AMD", "AMD Radeon Pro 5600M", "MacOS"),
            new GpuConfig("AMD", "AMD Radeon Pro Vega 56", "MacOS"),

            // Linux GPUs (OpenGL/Vulkan)
            new GpuConfig("NVIDIA Corporation", "NVIDIA GeForce RTX 4090/PCIe/SSE2", "Linux"),
            new GpuConfig("NVIDIA Corporation", "NVIDIA GeForce RTX 4080/PCIe/SSE2", "Linux"),
            new GpuConfig("NVIDIA Corporation", "NVIDIA GeForce RTX 4070/PCIe/SSE2", "Linux"),
            new GpuConfig("NVIDIA Corporation", "NVIDIA GeForce RTX 3090/PCIe/SSE2", "Linux"),
            new GpuConfig("NVIDIA Corporation", "NVIDIA GeForce RTX 3080/PCIe/SSE2", "Linux"),
            new GpuConfig("NVIDIA Corporation", "NVIDIA GeForce RTX 3070/PCIe/SSE2", "Linux"),
            new GpuConfig("AMD", "AMD Radeon RX 7900 XTX (RADV NAVI31)", "Linux"),
            new GpuConfig("AMD", "AMD Radeon RX 6900 XT (RADV NAVI21)", "Linux"),
            new GpuConfig("AMD", "AMD Radeon RX 6800 XT (RADV NAVI21)", "Linux"),
            new GpuConfig("Intel Open Source Technology Center", "Mesa Intel(R) UHD Graphics 770 (ADL-S GT1)", "Linux"),
            new GpuConfig("Intel Open Source Technology Center", "Mesa Intel(R) Iris Xe Graphics (TGL GT2)", "Linux"),
        };

        /// <summary>
        /// 获取随机 GPU 配置（指定操作系统）
        /// </summary>
        public GpuConfig GetRandomGpu(string os = "Windows")
        {
            var filtered = GpuConfigs.Where(g => g.OS == os).ToList();
            if (filtered.Count == 0)
            {
                filtered = GpuConfigs.Where(g => g.OS == "Windows").ToList(); // 回退到 Windows
            }
            return filtered[_random.Next(filtered.Count)];
        }

        /// <summary>
        /// 获取多个随机 GPU 配置
        /// </summary>
        public List<GpuConfig> GetRandomGpus(string os = "Windows", int count = 10)
        {
            var filtered = GpuConfigs.Where(g => g.OS == os).ToList();
            if (filtered.Count == 0)
            {
                filtered = GpuConfigs.Where(g => g.OS == "Windows").ToList();
            }

            var result = new List<GpuConfig>();
            for (int i = 0; i < Math.Min(count, filtered.Count); i++)
            {
                var gpu = filtered[_random.Next(filtered.Count)];
                if (!result.Any(g => g.Vendor == gpu.Vendor && g.Renderer == gpu.Renderer))
                {
                    result.Add(gpu);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取所有 GPU 配置（指定操作系统）
        /// </summary>
        public List<GpuConfig> GetAllGpus(string os = null)
        {
            if (string.IsNullOrEmpty(os))
            {
                return GpuConfigs.ToList();
            }
            return GpuConfigs.Where(g => g.OS == os).ToList();
        }

        /// <summary>
        /// 获取 GPU 配置总数
        /// </summary>
        public int GetTotalCount(string os = null)
        {
            if (string.IsNullOrEmpty(os))
            {
                return GpuConfigs.Count;
            }
            return GpuConfigs.Count(g => g.OS == os);
        }
    }

    /// <summary>
    /// GPU 配置数据结构
    /// </summary>
    public class GpuConfig
    {
        public string Vendor { get; set; }
        public string Renderer { get; set; }
        public string OS { get; set; }

        public GpuConfig(string vendor, string renderer, string os)
        {
            Vendor = vendor;
            Renderer = renderer;
            OS = os;
        }
    }
}
