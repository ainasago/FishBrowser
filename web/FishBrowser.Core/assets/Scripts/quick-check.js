// ============================================================================
// 快速检查脚本 - 在控制台运行以快速诊断问题
// 使用方法：复制粘贴到浏览器控制台，按回车
// ============================================================================

console.log('========================================');
console.log('🔍 Quick Fingerprint Check');
console.log('========================================\n');

// 1. Platform & Vendor
const platform = navigator.platform;
const vendor = navigator.vendor;
const expectedVendor = (platform === 'iPhone' || platform === 'iPad' || platform === 'iPod' || platform === 'MacIntel') 
    ? 'Apple Computer, Inc.' 
    : 'Google Inc.';

console.log('1️⃣ Platform & Vendor:');
console.log(`   Platform: ${platform}`);
console.log(`   Vendor: ${vendor}`);
console.log(`   Expected: ${expectedVendor}`);
console.log(`   Status: ${vendor === expectedVendor ? '✅ MATCH' : '❌ MISMATCH'}\n`);

// 2. webdriver
console.log('2️⃣ webdriver:');
console.log(`   Value: ${navigator.webdriver}`);
console.log(`   Status: ${navigator.webdriver === undefined ? '✅ GOOD (undefined)' : '❌ BAD (detected)'}\n`);

// 3. Chrome object
console.log('3️⃣ Chrome object:');
console.log(`   chrome.app: ${window.chrome?.app ? '✅' : '❌'}`);
console.log(`   chrome.csi: ${typeof window.chrome?.csi === 'function' ? '✅' : '❌'}`);
console.log(`   chrome.loadTimes: ${typeof window.chrome?.loadTimes === 'function' ? '✅' : '❌'}`);
console.log(`   chrome.runtime: ${window.chrome?.runtime ? '⚠️ EXISTS' : '✅ NONE'}\n`);

// 4. Plugins
console.log('4️⃣ Plugins:');
console.log(`   Count: ${navigator.plugins.length}`);
if (navigator.plugins.length > 0) {
    console.log(`   Names: ${Array.from(navigator.plugins).map(p => p.name).join(', ')}`);
}
console.log(`   Status: ${navigator.plugins.length > 0 ? '✅' : '⚠️ EMPTY'}\n`);

// 5. User-Agent
console.log('5️⃣ User-Agent:');
const ua = navigator.userAgent;
console.log(`   ${ua.substring(0, 80)}...`);
console.log(`   HeadlessChrome: ${ua.includes('HeadlessChrome') ? '❌ DETECTED' : '✅ CLEAN'}\n`);

// 6. Hardware
console.log('6️⃣ Hardware:');
console.log(`   hardwareConcurrency: ${navigator.hardwareConcurrency}`);
console.log(`   deviceMemory: ${navigator.deviceMemory}`);
console.log(`   maxTouchPoints: ${navigator.maxTouchPoints}\n`);

// 7. Automation traces
console.log('7️⃣ Automation traces:');
const traces = ['__webdriver_script_fn', '__playwright', '$cdc_asdjflasutopfhvcZLmcfl_', '_selenium'];
let found = 0;
traces.forEach(prop => {
    if (window[prop] !== undefined) {
        console.log(`   ❌ Found: ${prop}`);
        found++;
    }
});
if (found === 0) {
    console.log(`   ✅ No traces found`);
}
console.log('');

// 8. Performance
console.log('8️⃣ Performance:');
const navEntries = performance.getEntriesByType('navigation');
console.log(`   Navigation entries: ${navEntries.length} ${navEntries.length > 0 ? '✅' : '⚠️'}\n`);

// 总结
console.log('========================================');
const issues = [];
if (vendor !== expectedVendor) issues.push('Vendor mismatch');
if (navigator.webdriver !== undefined) issues.push('webdriver detected');
if (!window.chrome?.app) issues.push('chrome.app missing');
if (navigator.plugins.length === 0) issues.push('No plugins');
if (ua.includes('HeadlessChrome')) issues.push('HeadlessChrome in UA');
if (found > 0) issues.push(`${found} automation traces`);

if (issues.length === 0) {
    console.log('✅ All checks passed! Fingerprint looks good.');
} else {
    console.log(`❌ Found ${issues.length} issue(s):`);
    issues.forEach(issue => console.log(`   - ${issue}`));
}
console.log('========================================');
