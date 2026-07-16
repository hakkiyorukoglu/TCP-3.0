const fs = require('fs');
const readline = require('readline');

async function processLineByLine() {
  const fileStream = fs.createReadStream('C:\\Users\\yoruk\\.gemini\\antigravity\\brain\\ca31125c-e38f-45c4-a69a-24dbb2c0d0bc\\.system_generated\\logs\\transcript_full.jsonl');
  const rl = readline.createInterface({ input: fileStream, crlfDelay: Infinity });

  for await (const line of rl) {
    if (line.includes('TEST BORCU KAPATMA')) {
      const obj = JSON.parse(line);
      if (obj.type === 'USER_INPUT') {
        fs.writeFileSync('muhur_talimati.txt', obj.content);
        console.log('Yazıldı.');
        break;
      }
    }
  }
}
processLineByLine();
