/**
 * 操作录制器 - 捕获用户操作并转换为 DSL 脚本
 */
(function() {
    'use strict';

    // 避免重复注入
    if (window.__recorder) {
        return;
    }

    const Recorder = {
        isRecording: false,
        actions: [],
        lastInputTime: {},
        inputDebounceMs: 500,
        
        // 初始化
        init() {
            this.attachEvents();
            console.log('[Recorder] Initialized');
        },
        
        // 附加事件监听
        attachEvents() {
            // 点击事件
            document.addEventListener('click', this.onClick.bind(this), true);
            
            // 输入事件
            document.addEventListener('input', this.onInput.bind(this), true);
            document.addEventListener('change', this.onChange.bind(this), true);
            
            // 导航事件
            this.interceptNavigation();
            
            // 表单提交
            document.addEventListener('submit', this.onSubmit.bind(this), true);
        },
        
        // 点击事件
        onClick(e) {
            if (!this.isRecording) return;
            
            const element = e.target;
            
            // 忽略某些元素
            if (this.shouldIgnoreElement(element)) {
                return;
            }
            
            const selector = this.generateSelector(element);
            const action = {
                type: 'click',
                selector: selector,
                element: {
                    tagName: element.tagName,
                    text: element.textContent?.substring(0, 50),
                    href: element.href
                },
                timestamp: Date.now()
            };
            
            this.addAction(action);
            console.log('[Recorder] Click:', action);
        },
        
        // 输入事件（防抖）
        onInput(e) {
            if (!this.isRecording) return;
            
            const element = e.target;
            if (!['INPUT', 'TEXTAREA'].includes(element.tagName)) {
                return;
            }
            
            const selector = this.generateSelector(element);
            const selectorKey = JSON.stringify(selector);
            
            // 清除之前的定时器
            if (this.lastInputTime[selectorKey]) {
                clearTimeout(this.lastInputTime[selectorKey].timer);
            }
            
            // 设置新的定时器
            this.lastInputTime[selectorKey] = {
                value: element.value,
                timer: setTimeout(() => {
                    const action = {
                        type: element.type === 'password' ? 'fill_password' : 'fill',
                        selector: selector,
                        value: element.type === 'password' ? '***' : element.value,
                        element: {
                            tagName: element.tagName,
                            type: element.type,
                            placeholder: element.placeholder
                        },
                        timestamp: Date.now()
                    };
                    
                    this.addAction(action);
                    console.log('[Recorder] Input:', action);
                }, this.inputDebounceMs)
            };
        },
        
        // 变更事件（select、checkbox、radio）
        onChange(e) {
            if (!this.isRecording) return;
            
            const element = e.target;
            if (!['SELECT', 'INPUT'].includes(element.tagName)) {
                return;
            }
            
            const selector = this.generateSelector(element);
            let action;
            
            if (element.tagName === 'SELECT') {
                action = {
                    type: 'select',
                    selector: selector,
                    value: element.value,
                    element: {
                        tagName: element.tagName
                    },
                    timestamp: Date.now()
                };
            } else if (element.type === 'checkbox') {
                action = {
                    type: element.checked ? 'check' : 'uncheck',
                    selector: selector,
                    element: {
                        tagName: element.tagName,
                        type: element.type
                    },
                    timestamp: Date.now()
                };
            } else if (element.type === 'radio') {
                action = {
                    type: 'check',
                    selector: selector,
                    element: {
                        tagName: element.tagName,
                        type: element.type,
                        value: element.value
                    },
                    timestamp: Date.now()
                };
            }
            
            if (action) {
                this.addAction(action);
                console.log('[Recorder] Change:', action);
            }
        },
        
        // 表单提交
        onSubmit(e) {
            if (!this.isRecording) return;
            
            const form = e.target;
            const selector = this.generateSelector(form);
            
            const action = {
                type: 'submit',
                selector: selector,
                element: {
                    tagName: form.tagName,
                    action: form.action
                },
                timestamp: Date.now()
            };
            
            this.addAction(action);
            console.log('[Recorder] Submit:', action);
        },
        
        // 拦截导航
        interceptNavigation() {
            const originalPushState = history.pushState;
            const originalReplaceState = history.replaceState;
            
            history.pushState = (...args) => {
                if (this.isRecording) {
                    this.recordNavigation(args[2] || location.href);
                }
                return originalPushState.apply(history, args);
            };
            
            history.replaceState = (...args) => {
                if (this.isRecording) {
                    this.recordNavigation(args[2] || location.href);
                }
                return originalReplaceState.apply(history, args);
            };
            
            window.addEventListener('popstate', () => {
                if (this.isRecording) {
                    this.recordNavigation(location.href);
                }
            });
        },
        
        // 记录导航
        recordNavigation(url) {
            const action = {
                type: 'navigate',
                url: url,
                timestamp: Date.now()
            };
            
            this.addAction(action);
            console.log('[Recorder] Navigate:', action);
        },
        
        // 添加动作
        addAction(action) {
            // 合并相似的连续动作
            const lastAction = this.actions[this.actions.length - 1];
            
            if (lastAction && this.canMergeActions(lastAction, action)) {
                // 更新最后一个动作
                lastAction.value = action.value;
                lastAction.timestamp = action.timestamp;
            } else {
                this.actions.push(action);
            }
            
            // 发送到宿主
            this.sendToHost({
                type: 'action_recorded',
                action: action,
                totalActions: this.actions.length
            });
        },
        
        // 判断是否可以合并动作
        canMergeActions(action1, action2) {
            if (action1.type !== action2.type) return false;
            if (action1.type !== 'fill' && action1.type !== 'fill_password') return false;
            if (JSON.stringify(action1.selector) !== JSON.stringify(action2.selector)) return false;
            if (action2.timestamp - action1.timestamp > 2000) return false;
            return true;
        },
        
        // 生成选择器
        generateSelector(element) {
            // 复用选择器拾取器的逻辑
            if (element.id) {
                return { type: 'css', value: `#${element.id}` };
            }
            
            if (element.hasAttribute('data-testid')) {
                return { type: 'css', value: `[data-testid="${element.getAttribute('data-testid')}"]` };
            }
            
            if (element.name) {
                return { type: 'css', value: `[name="${element.name}"]` };
            }
            
            if (element.placeholder) {
                return { type: 'placeholder', value: element.placeholder };
            }
            
            if (['A', 'BUTTON'].includes(element.tagName)) {
                const text = element.textContent?.trim();
                if (text && text.length < 50) {
                    return { type: 'text', value: text };
                }
            }
            
            return { type: 'css', value: this.getCssPath(element) };
        },
        
        // 获取 CSS 路径
        getCssPath(element) {
            const path = [];
            let current = element;
            
            while (current && current !== document.body) {
                let selector = current.tagName.toLowerCase();
                
                if (current.id) {
                    selector += `#${current.id}`;
                    path.unshift(selector);
                    break;
                }
                
                if (current.className) {
                    const classes = current.className.split(' ').filter(c => c.trim());
                    if (classes.length > 0) {
                        selector += '.' + classes.slice(0, 2).join('.');
                    }
                }
                
                path.unshift(selector);
                current = current.parentElement;
            }
            
            return path.join(' > ');
        },
        
        // 判断是否应该忽略元素
        shouldIgnoreElement(element) {
            // 忽略录制器自己的元素
            if (element.id && element.id.startsWith('__selector-picker')) {
                return true;
            }
            
            // 忽略某些标签
            const ignoreTags = ['HTML', 'BODY', 'HEAD', 'SCRIPT', 'STYLE'];
            if (ignoreTags.includes(element.tagName)) {
                return true;
            }
            
            return false;
        },
        
        // 发送消息到宿主
        sendToHost(data) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify(data));
            } else {
                console.log('[Recorder] Message:', data);
            }
        },
        
        // 开始录制
        start() {
            this.isRecording = true;
            this.actions = [];
            this.lastInputTime = {};
            
            // 记录初始 URL
            this.addAction({
                type: 'navigate',
                url: location.href,
                timestamp: Date.now()
            });
            
            console.log('[Recorder] Started');
            this.sendToHost({ type: 'recording_started' });
        },
        
        // 停止录制
        stop() {
            this.isRecording = false;
            
            // 清除所有定时器
            Object.values(this.lastInputTime).forEach(item => {
                if (item.timer) clearTimeout(item.timer);
            });
            this.lastInputTime = {};
            
            console.log('[Recorder] Stopped, total actions:', this.actions.length);
            this.sendToHost({
                type: 'recording_stopped',
                actions: this.actions
            });
            
            return this.actions;
        },
        
        // 获取动作列表
        getActions() {
            return this.actions;
        },
        
        // 清空动作
        clear() {
            this.actions = [];
            this.lastInputTime = {};
        }
    };
    
    // 初始化
    Recorder.init();
    
    // 暴露到全局
    window.__recorder = Recorder;
    
    console.log('[Recorder] Script loaded');
})();
