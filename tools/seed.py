"""Seed the TodoList data file with sample tasks (UTF-8 source is safe here)."""
import os
import base64

data_dir = os.path.join(os.environ["APPDATA"], "BlueFatFishTodo")
os.makedirs(data_dir, exist_ok=True)

TASKS = [
    (1, 0, "工作", "写项目周报"),
    (2, 0, "生活", "买鱼食"),
    (3, 0, "学习", "读完 PowerShell 实战"),
    (4, 1, "工作", "回复客户邮件"),
    (5, 0, "学习", "复习正则表达式"),
    (6, 1, "生活", "给鱼换水"),
]


def enc(s):
    return base64.b64encode(s.encode("utf-8")).decode("ascii")


path = os.path.join(data_dir, "tasks.dat")
with open(path, "w", encoding="utf-8", newline="") as f:
    for i, done, cat, text in TASKS:
        f.write("%d\t%d\t%s\t%s\r\n" % (i, done, enc(cat), enc(text)))

print("seeded %d tasks -> %s" % (len(TASKS), path))
