[README.md](https://github.com/user-attachments/files/33731401/README.md)
# 蓝色大肥鱼 TodoList

Windows 桌面待办工具。**单文件 EXE，双击即用，不需要安装 Python / .NET / 任何依赖。**

## 运行

双击 `BlueFishTodo.exe`（或 `todolist\dist\BlueFishTodo.exe`）。

## 功能对照

| 需求 | 实现方式 |
|---|---|
| 添加任务 | 输入后按 **回车** 或点 **添加** |
| 删除任务 | 点任务右侧的 **×** |
| 标记完成 | **点任务文字**（或左侧圆圈）切换，完成显示**划线+置灰** |
| 任务总数 | 顶部「共 N 项」、底部「共 N 项」 |
| 未完成数 | 顶部「未完成 N 项」、底部同步 |
| 清除已完成 | 右下 **清除已完成**（会先确认） |
| 保存数据 | 每次改动立即落盘 |
| 最小化 | 最小化后弹出屏幕右下角的浮窗，列出**未完成的前 10 条**；**单击浮窗**回到主窗口 |
| 任务分类 | 输入时在「分类」处填写或选择；顶部「筛选」可按分类过滤，标签会显示在每行右侧 |
| 拖拽排序 | **按住任务行上下拖动**即可调整优先级，顺序即时保存 |
| 双击编辑 | **双击任务文字**进入原地编辑，回车确认 / Esc 取消 |
| 封装 EXE | 已编译为 `BlueFishTodo.exe` |

## 数据存放在哪

```
%APPDATA%\BlueFatFishTodo\tasks.dat
```

即 `C:\Users\<你的用户名>\AppData\Roaming\BlueFatFishTodo\tasks.dat`。

每行一条任务，格式为 `id \t 完成标志 \t base64(分类) \t base64(文字)`。
用 base64 是为了让任意中文、制表符、换行都不会破坏格式。**删掉这个文件就等于清空全部任务**；
备份它就是备份数据（可以随 EXE 一起拷到别的机器）。

## 实现要点

- **不依赖任何第三方库**：只用 .NET Framework 自带的 WinForms + GDI+，用 Windows 自带的
  `csc.exe` 编译。所以不用联网、不用装包，EXE 只有 ~52 KB。
- **自绘任务行**（`TaskRow`）：圆角卡片、圆形勾选、分类胶囊、划线文字、悬停高亮的 ×。
- **拖拽排序**利用 `FlowLayoutPanel` 的子控件顺序 —— 控件顺序即优先级顺序，
  拖动时只调 `SetChildIndex` 并同步 `_tasks`，不销毁控件，所以拖拽不会闪也不会崩。
- **单击切换 / 双击编辑**靠一个 200ms 的判定定时器区分：双击会取消待执行的切换，
  避免"点两下把状态切了又弹回来"的抖动。
- **删除/重命名**都通过 `BeginInvoke` 延后到事件返回之后再重建控件，
  避免在控件自己的鼠标事件里把它 `Dispose` 掉。

## 重新构建

```powershell
.\build.ps1
```

流程：Python + Pillow 从 `tools\head-source.png` 抠出角色头像（深色背景，用**边缘 flood fill + 颜色距离**，
所以近黑色的眼睛会被保留）→ 生成 `build\head.png`（界面里的头像）和 `build\head.ico`（多尺寸图标）
→ `csc.exe` 把两者都编进 EXE → `dist\BlueFishTodo.exe`。

**想换头像**：直接替换 `tools\head-source.png` 再跑一次 `build.ps1` 即可，代码不用改。
如果那张图不是深色背景，把 `make_head.py` 顶部注释里的阈值调一下（或改回按白底判断）。

## 目录

```
todolist/
├── build.ps1              一键构建
├── src\Todo.cs            全部程序代码（单文件）
├── tools\head-source.png  角色原图（头像来源）
├── tools\make_head.py     抠背景 -> head.png + head.ico
├── tools\seed.py          写入演示数据（测试用）
├── verify.ps1             自动验证脚本（点按 / 最小化 / 持久化）
├── build\                 中间产物与验证截图
└── dist\BlueFishTodo.exe  成品
```

## 小提示

- 首次运行 Windows 可能弹 SmartScreen（EXE 没有数字签名），点「更多信息 → 仍要运行」。
- 窗口可以拉伸，最小 470×540；任务多时列表自动出现滚动条。
- 顶部计数是**全局**的，不受「筛选」影响；筛选只改变下面显示哪些任务。
