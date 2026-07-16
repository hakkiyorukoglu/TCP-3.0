import json
with open(r'C:\Users\yoruk\.gemini\antigravity\brain\ca31125c-e38f-45c4-a69a-24dbb2c0d0bc\.system_generated\logs\transcript_full.jsonl', 'r', encoding='utf-8') as f:
    for line in f:
        if 'TEST BORCU KAPATMA' in line:
            obj = json.loads(line)
            if obj.get('type') == 'USER_INPUT':
                with open('muhur_talimati.txt', 'w', encoding='utf-8') as out:
                    out.write(obj.get('content'))
                print('Yazıldı.')
                break
