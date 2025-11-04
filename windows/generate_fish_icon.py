#!/usr/bin/env python3
"""
生成"鱼纹浏览器"应用图标 - 带有鱼的设计
"""
from PIL import Image, ImageDraw
import math
import os

def draw_fish(draw, center_x, center_y, size, color, direction=1):
    """
    绘制一条鱼
    direction: 1 表示向右，-1 表示向左
    """
    # 鱼身（椭圆）
    body_width = size * 0.6
    body_height = size * 0.35
    
    # 确保坐标正确（x1 < x2, y1 < y2）
    x_left = center_x - body_width / 2
    x_right = center_x + body_width / 2
    y_top = center_y - body_height / 2
    y_bottom = center_y + body_height / 2
    
    # 根据方向调整
    if direction == -1:
        x_left, x_right = x_right, x_left
    
    draw.ellipse([min(x_left, x_right), y_top, max(x_left, x_right), y_bottom], 
                 fill=color, outline=(255, 255, 255, 200), width=2)
    
    # 鱼尾（三角形）
    tail_x = center_x + body_width * direction / 2
    tail_y = center_y
    tail_size = size * 0.25
    
    tail_points = [
        (tail_x, tail_y - tail_size),
        (tail_x + tail_size * direction, tail_y),
        (tail_x, tail_y + tail_size),
    ]
    draw.polygon(tail_points, fill=color, outline=(255, 255, 255, 200))
    
    # 鱼眼
    eye_x = center_x - body_width * direction * 0.25
    eye_y = center_y - body_height * 0.3
    eye_size = size * 0.08
    draw.ellipse(
        [eye_x - eye_size, eye_y - eye_size, eye_x + eye_size, eye_y + eye_size],
        fill=(255, 255, 255, 255),
        outline=(0, 0, 0, 255),
        width=1
    )
    
    # 眼珠
    pupil_size = size * 0.04
    draw.ellipse(
        [eye_x - pupil_size, eye_y - pupil_size, eye_x + pupil_size, eye_y + pupil_size],
        fill=(0, 0, 0, 255)
    )
    
    # 鱼鳍（背鳍）
    fin_x = center_x - body_width * direction * 0.1
    fin_y = center_y - body_height / 2
    fin_size = size * 0.15
    
    fin_points = [
        (fin_x, fin_y),
        (fin_x - fin_size * 0.3 * direction, fin_y - fin_size),
        (fin_x + fin_size * 0.3 * direction, fin_y),
    ]
    draw.polygon(fin_points, fill=(200, 100, 255, 200), outline=(255, 255, 255, 150))

# 创建 256x256 的图像
size = 256
img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# 背景渐变色（从深蓝到浅蓝 - 水的感觉）
for y in range(size):
    # 计算渐变色
    ratio = y / size
    r = int(20 + (60 - 20) * ratio)
    g = int(80 + (150 - 80) * ratio)
    b = int(180 + (220 - 180) * ratio)
    draw.line([(0, y), (size, y)], fill=(r, g, b, 255))

# 添加水波纹效果
wave_color = (100, 150, 200, 80)
for i in range(3):
    y = 30 + i * 60
    draw.line([(0, y), (size, y)], fill=wave_color, width=2)

# 绘制主鱼（向右）- 金色
main_fish_color = (255, 200, 50, 255)
draw_fish(draw, size // 2, size // 2 - 20, 80, main_fish_color, direction=1)

# 绘制小鱼1（向左上）- 紫色
small_fish_color1 = (200, 100, 255, 200)
draw_fish(draw, size * 0.25, size * 0.3, 40, small_fish_color1, direction=-1)

# 绘制小鱼2（向右下）- 青色
small_fish_color2 = (100, 200, 255, 200)
draw_fish(draw, size * 0.75, size * 0.7, 40, small_fish_color2, direction=1)

# 添加气泡装饰
bubble_positions = [
    (size * 0.2, size * 0.5),
    (size * 0.8, size * 0.3),
    (size * 0.5, size * 0.8),
]

for bx, by in bubble_positions:
    bubble_size = 8
    draw.ellipse(
        [bx - bubble_size, by - bubble_size, bx + bubble_size, by + bubble_size],
        outline=(200, 220, 255, 150),
        width=2
    )

# 保存为 PNG
output_dir = os.path.dirname(os.path.abspath(__file__))
png_path = os.path.join(output_dir, 'fish_browser_icon.png')
img.save(png_path, 'PNG')
print(f"✅ PNG icon saved: {png_path}")

# 转换为 ICO（多个尺寸）
ico_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
ico_images = []

for ico_size in ico_sizes:
    resized = img.resize(ico_size, Image.Resampling.LANCZOS)
    ico_images.append(resized)

ico_path = os.path.join(output_dir, 'fish_browser_icon.ico')
ico_images[0].save(ico_path, 'ICO', sizes=ico_sizes, append_images=ico_images[1:])
print(f"✅ ICO icon saved: {ico_path}")

print("\n🐟 Fish icon generation complete!")
print(f"   PNG: {png_path}")
print(f"   ICO: {ico_path}")
