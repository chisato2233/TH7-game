## 第一阶段工作


1. 参考发出的示例文明文件夹根目录的README.md，了解初步各自负责的文明的文件结构
2. 配置：在文明根目录完成 faction.yaml 配置文件
3. 创建heros根目录，制作 1 - 2个文明内部Hero的美术资源，对于每一个Hero：
   1. 设计一个全局唯一id 【英文，只允许出现 "a-z,A-Z,0-9,_"这些字符】
   2. 以全局id为命名创建此英雄的文件夹
   3. 配置：在此Hero的根目录完成 config.yaml 配置文件
   4. 图片：创建portrait_{width}x{height}.png 作为UI头像使用
   5. 图片：创建worldsprite_{width}x{height}.png，作为渲染目标使用
   6. 动画：创建基于 worldsprite 的 idle 和 move动画, 60帧序列帧，分解为透明背景png放在 `animations/{idle或move}_anim_image`

截止日期：1月25日
