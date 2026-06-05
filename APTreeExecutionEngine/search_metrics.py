import json

path = r'c:\Users\sherk\AppData\Roaming\Code\User\workspaceStorage\4c597b79f908f412b4962bcf25396e3e\GitHub.copilot-chat\transcripts\91cc262c-2220-4141-86d0-7682e16a7684.jsonl'
with open(path, encoding='utf-8', errors='replace') as f:
    lines = f.readlines()

# Search for M1/M2/M3 metric definitions
for i, line in enumerate(lines):
    try:
        obj = json.loads(line)
        text = str(obj)
        if ('M_1' in text or 'M_2' in text or 'M_3' in text or 
            't_{fault}' in text or 't_{resume}' in text or
            ('metric' in text.lower() and 'fault' in text.lower())):
            print(f"=== LINE {i} ===")
            # Find the relevant part
            for keyword in ['M_1', 'M_2', 'M_3', 't_{fault}', 'recovery success', 'replan latency']:
                idx = text.find(keyword)
                if idx >= 0:
                    print(f"  [{keyword}] ...{text[max(0,idx-50):idx+200]}...")
                    break
            print()
    except:
        pass
