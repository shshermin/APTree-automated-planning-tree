import json

path = r'c:\Users\sherk\AppData\Roaming\Code\User\workspaceStorage\4c597b79f908f412b4962bcf25396e3e\GitHub.copilot-chat\transcripts\91cc262c-2220-4141-86d0-7682e16a7684.jsonl'
with open(path, encoding='utf-8', errors='replace') as f:
    lines = f.readlines()

# Line 894 (0-indexed) is the user message with metric definitions
obj = json.loads(lines[894])
print(obj['data']['content'])
