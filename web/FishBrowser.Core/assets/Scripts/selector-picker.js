/**
 * 选择器拾取器 - 用于可视化选择页面元素并生成稳健的选择器
 */
(function() {
    'use strict';

    // 避免重复注入
    if (window.__selectorPicker) {
        return;
    }

    const SelectorPicker = {
        isActive: false,
        overlay: null,
        highlightBox: null,
        infoBox: null,
        currentElement: null,
        
        // 初始化
        init() {
            this.createOverlay();
            this.createHighlightBox();
            this.createInfoBox();
            this.attachEvents();
            console.log('[SelectorPicker] Initialized');
        },
        
        // 创建覆盖层
        createOverlay() {
            this.overlay = document.createElement('div');
            this.overlay.id = '__selector-picker-overlay';
            this.overlay.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                z-index: 999998;
                cursor: crosshair;
                pointer-events: none;
            `;
            document.body.appendChild(this.overlay);
        },
        
        // 创建高亮框
        createHighlightBox() {
            this.highlightBox = document.createElement('div');
            this.highlightBox.id = '__selector-picker-highlight';
            this.highlightBox.style.cssText = `
                position: fixed;
                pointer-events: none;
                z-index: 9999999;
                display: none;
                box-sizing: border-box;
            `;
            document.body.appendChild(this.highlightBox);
        },
        
        // 创建信息框
        createInfoBox() {
            this.infoBox = document.createElement('div');
            this.infoBox.id = '__selector-picker-info';
            this.infoBox.style.cssText = `
                position: absolute;
                background: #10A37F;
                color: white;
                padding: 6px 12px;
                border-radius: 4px;
                font-family: 'Consolas', monospace;
                font-size: 12px;
                pointer-events: none;
                z-index: 1000000;
                display: none;
                box-shadow: 0 2px 8px rgba(0,0,0,0.2);
                max-width: 400px;
                word-break: break-all;
            `;
            document.body.appendChild(this.infoBox);
        },
        
        // 附加事件
        attachEvents() {
            // 在覆盖层上监听事件（使用捕获阶段）
            this.overlay.addEventListener('mousemove', this.onMouseMove.bind(this), true);
            this.overlay.addEventListener('click', this.onClick.bind(this), true);
            
            // 在文档上监听键盘事件
            document.addEventListener('keydown', this.onKeyDown.bind(this), true);
        },
        
        // 鼠标移动
        onMouseMove(e) {
            if (!this.isActive) return;
            
            e.stopPropagation();
            const element = document.elementFromPoint(e.clientX, e.clientY);
            
            if (!element || element === this.overlay || element === this.highlightBox || element === this.infoBox) {
                return;
            }
            
            this.currentElement = element;
            this.highlightElement(element, e.clientX, e.clientY);
        },
        
        // 点击
        onClick(e) {
            console.log('[SelectorPicker] onClick triggered, isActive:', this.isActive);
            
            if (!this.isActive) return;
            
            e.preventDefault();
            e.stopPropagation();
            
            // 临时隐藏覆盖层以获取真实的元素
            const overlayDisplay = this.overlay.style.display;
            const highlightDisplay = this.highlightBox.style.display;
            const infoDisplay = this.infoBox.style.display;
            
            this.overlay.style.display = 'none';
            this.highlightBox.style.display = 'none';
            this.infoBox.style.display = 'none';
            
            // 获取点击位置的真实元素
            const element = document.elementFromPoint(e.clientX, e.clientY);
            console.log('[SelectorPicker] Element at click position:', element?.tagName, element?.id, element?.className);
            
            // 恢复覆盖层显示
            this.overlay.style.display = overlayDisplay;
            this.highlightBox.style.display = highlightDisplay;
            this.infoBox.style.display = infoDisplay;
            
            if (element && element !== document.body && element !== document.documentElement) {
                this.currentElement = element;
            }
            
            console.log('[SelectorPicker] currentElement:', this.currentElement?.tagName, this.currentElement?.id);
            
            if (this.currentElement) {
                const selector = this.generateSelector(this.currentElement);
                console.log('[SelectorPicker] Generated selector:', selector);
                
                const message = {
                    type: 'selector_picked',
                    selector: selector,
                    element: {
                        tagName: this.currentElement.tagName,
                        id: this.currentElement.id,
                        className: this.currentElement.className,
                        text: this.currentElement.textContent?.substring(0, 50)
                    }
                };
                
                console.log('[SelectorPicker] Sending message:', message);
                this.sendToHost(message);
                
                console.log('[SelectorPicker] Selected:', selector);
                this.deactivate();
            } else {
                console.log('[SelectorPicker] No current element!');
            }
        },
        
        // 键盘事件
        onKeyDown(e) {
            if (!this.isActive) return;
            
            if (e.key === 'Escape') {
                e.preventDefault();
                this.deactivate();
                this.sendToHost({ type: 'picker_cancelled' });
            }
        },
        
        // 获取元素类型对应的颜色
        getElementColor(element) {
            const tag = element.tagName.toLowerCase();
            const colors = {
                // 容器元素 - 蓝色
                'div': '#3B82F6',
                'section': '#3B82F6',
                'article': '#3B82F6',
                'main': '#3B82F6',
                'header': '#3B82F6',
                'footer': '#3B82F6',
                'nav': '#3B82F6',
                'aside': '#3B82F6',
                
                // 表单元素 - 红色
                'input': '#EF4444',
                'textarea': '#EF4444',
                'select': '#EF4444',
                'button': '#EF4444',
                'form': '#EF4444',
                
                // 文本元素 - 绿色
                'p': '#10B981',
                'span': '#10B981',
                'a': '#10B981',
                'h1': '#10B981',
                'h2': '#10B981',
                'h3': '#10B981',
                'h4': '#10B981',
                'h5': '#10B981',
                'h6': '#10B981',
                'label': '#10B981',
                
                // 列表元素 - 紫色
                'ul': '#A855F7',
                'ol': '#A855F7',
                'li': '#A855F7',
                'table': '#A855F7',
                'tr': '#A855F7',
                'td': '#A855F7',
                'th': '#A855F7',
                
                // 媒体元素 - 橙色
                'img': '#F97316',
                'video': '#F97316',
                'audio': '#F97316',
                'canvas': '#F97316',
                
                // 默认 - 青色
                'default': '#06B6D4'
            };
            
            return colors[tag] || colors['default'];
        },
        
        // 高亮元素
        highlightElement(element, mouseX, mouseY) {
            const rect = element.getBoundingClientRect();
            const color = this.getElementColor(element);
            
            // 设置高亮框样式（使用 fixed 定位，坐标直接使用 getBoundingClientRect）
            this.highlightBox.style.display = 'block';
            this.highlightBox.style.left = rect.left + 'px';
            this.highlightBox.style.top = rect.top + 'px';
            this.highlightBox.style.width = rect.width + 'px';
            this.highlightBox.style.height = rect.height + 'px';
            this.highlightBox.style.border = `2px solid ${color}`;
            this.highlightBox.style.backgroundColor = color + '26'; // 15% 透明度
            this.highlightBox.style.boxShadow = `0 0 0 1px ${color}, inset 0 0 0 1px ${color}`;
            
            const selector = this.generateSelector(element);
            const tagName = element.tagName.toLowerCase();
            const className = element.className ? `.${element.className.split(' ').slice(0, 2).join('.')}` : '';
            const id = element.id ? `#${element.id}` : '';
            
            // 设置信息框样式
            this.infoBox.innerHTML = `
                <div style="font-weight: bold; color: ${color}; margin-bottom: 4px;">&lt;${tagName}${id}${className}&gt;</div>
                <div style="font-size: 11px; opacity: 0.9; line-height: 1.4;">${selector.value?.substring(0, 60)}...</div>
            `;
            this.infoBox.style.display = 'block';
            this.infoBox.style.border = `1px solid ${color}`;
            this.infoBox.style.backgroundColor = color + '33'; // 20% 透明度
            this.infoBox.style.color = '#fff';
            
            // 定位信息框（避免超出视口）
            let infoX = mouseX + 10;
            let infoY = mouseY + 10;
            
            if (infoX + 400 > window.innerWidth) {
                infoX = mouseX - 410;
            }
            if (infoY + 80 > window.innerHeight) {
                infoY = mouseY - 90;
            }
            
            this.infoBox.style.left = infoX + 'px';
            this.infoBox.style.top = infoY + 'px';
        },
        
        // 生成选择器（优先级：ID > data-testid > unique class > CSS path）
        generateSelector(element) {
            // 1. ID
            if (element.id) {
                return { type: 'css', value: `#${element.id}` };
            }
            
            // 2. data-testid
            if (element.hasAttribute('data-testid')) {
                return { type: 'css', value: `[data-testid="${element.getAttribute('data-testid')}"]` };
            }
            
            // 3. data-test
            if (element.hasAttribute('data-test')) {
                return { type: 'css', value: `[data-test="${element.getAttribute('data-test')}"]` };
            }
            
            // 4. name 属性（表单元素）
            if (element.name) {
                return { type: 'css', value: `[name="${element.name}"]` };
            }
            
            // 5. placeholder（输入框）
            if (element.placeholder) {
                return { type: 'placeholder', value: element.placeholder };
            }
            
            // 6. 唯一的 class 组合
            if (element.className) {
                const classes = element.className.split(' ').filter(c => c.trim());
                if (classes.length > 0) {
                    const selector = element.tagName.toLowerCase() + '.' + classes.join('.');
                    if (document.querySelectorAll(selector).length === 1) {
                        return { type: 'css', value: selector };
                    }
                }
            }
            
            // 7. 文本内容（按钮、链接）
            if (['A', 'BUTTON'].includes(element.tagName)) {
                const text = element.textContent?.trim();
                if (text && text.length < 50) {
                    return { type: 'text', value: text };
                }
            }
            
            // 8. CSS 路径
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
                
                // 添加 nth-child 以确保唯一性
                const parent = current.parentElement;
                if (parent) {
                    const siblings = Array.from(parent.children).filter(
                        child => child.tagName === current.tagName
                    );
                    if (siblings.length > 1) {
                        const index = siblings.indexOf(current) + 1;
                        selector += `:nth-child(${index})`;
                    }
                }
                
                path.unshift(selector);
                current = parent;
            }
            
            return path.join(' > ');
        },
        
        // 发送消息到宿主
        sendToHost(data) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify(data));
            } else {
                console.log('[SelectorPicker] Message:', data);
            }
        },
        
        // 激活
        activate() {
            this.isActive = true;
            this.overlay.style.display = 'block';
            this.overlay.style.pointerEvents = 'auto';
            document.body.style.cursor = 'crosshair';
            console.log('[SelectorPicker] Activated');
        },
        
        // 停用
        deactivate() {
            this.isActive = false;
            this.overlay.style.pointerEvents = 'none';
            this.highlightBox.style.display = 'none';
            this.infoBox.style.display = 'none';
            document.body.style.cursor = '';
            this.currentElement = null;
            console.log('[SelectorPicker] Deactivated');
        },
        
        // 清理
        cleanup() {
            if (this.overlay) this.overlay.remove();
            if (this.highlightBox) this.highlightBox.remove();
            if (this.infoBox) this.infoBox.remove();
            document.removeEventListener('mousemove', this.onMouseMove);
            document.removeEventListener('click', this.onClick);
            document.removeEventListener('keydown', this.onKeyDown);
            console.log('[SelectorPicker] Cleaned up');
        }
    };
    
    // 初始化
    SelectorPicker.init();
    
    // 暴露到全局
    window.__selectorPicker = SelectorPicker;
    
    console.log('[SelectorPicker] Script loaded');
})();
