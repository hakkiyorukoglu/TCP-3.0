const fs = require('fs');
const readline = require('readline');
async function run() {
  const fileStream = fs.createReadStream('C:\\Users\\yoruk\\.gemini\\antigravity\\brain\\ca31125c-e38f-45c4-a69a-24dbb2c0d0bc\\.system_generated\\logs\\transcript_full.jsonl');
  const rl = readline.createInterface({ input: fileStream, crlfDelay: Infinity });
  for await (const line of rl) {
    if (line.includes('T302_ZoomAt_ImlecAltiSabit')) {
      const obj = JSON.parse(line);
      if (obj.type === 'USER_INPUT') {
        fs.writeFileSync('onceki_talimat.txt', obj.content);
        console.log('Bulundu.');
        return;
      }
    }
  }
}
run();
