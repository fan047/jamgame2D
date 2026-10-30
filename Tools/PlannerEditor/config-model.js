(function(root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.PlannerModel = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function() {
  'use strict';
  const clone = value => JSON.parse(JSON.stringify(value));
  const object = value => value !== null && typeof value === 'object' && !Array.isArray(value);
  const integer = value => Number.isInteger(value) && value >= -2147483648 && value <= 2147483647;
  function validate(settings, level) {
    const errors = [];
    const fail = (path, message) => errors.push(`${path}：${message}`);
    function keys(obj, allowed, path) {
      Object.keys(obj).forEach(key => { if (!allowed.includes(key)) fail(`${path}.${key}`, '未知字段，请检查拼写'); });
    }
    function header(obj, path, allowed) {
      if (!object(obj)) { fail(path, '需要 JSON 对象'); return false; }
      keys(obj, allowed, path);
      if (obj.schemaVersion !== 1) fail(`${path}.schemaVersion`, '必须为 1');
      if (typeof obj.version !== 'string' || !/^[A-Za-z0-9._-]{1,32}$/.test(obj.version)) fail(`${path}.version`, '使用 1–32 个英文字母、数字、点、下划线或连字符');
      return true;
    }
    if (header(settings, 'settings.json', ['schemaVersion','version','ballsPerRound','ballRadius','ballSpeed','launchInterval','maxAimAngle','recallTimeout'])) {
      if (!integer(settings.ballsPerRound) || settings.ballsPerRound <= 0) fail('settings.json.ballsPerRound', '需要正整数（不超过 2147483647）');
      for (const key of ['ballRadius','ballSpeed','launchInterval','maxAimAngle','recallTimeout']) {
        const value = settings[key], max = key === 'ballRadius' ? 0.5 : key === 'maxAimAngle' ? 90 : Infinity;
        if (typeof value !== 'number' || !Number.isFinite(Math.fround(value)) || Math.fround(value) <= 0 || Math.fround(value) >= max)
          fail(`settings.json.${key}`, `需要有限正数${max === Infinity ? '' : `，且小于 ${max}`}`);
      }
    }
    if (!header(level, 'level.json', ['schemaVersion','version','initialBlocks','incomingRows'])) return errors;
    let targets = 0;
    function blocks(list, path, incoming, last) {
      if (!Array.isArray(list)) { fail(path, '需要方块数组'); return; }
      const occupied = new Set();
      list.forEach((b, index) => {
        const p = `${path}[${index}]`;
        if (!object(b)) { fail(p, '需要方块对象'); return; }
        keys(b, ['column','row','width','health','target'], p);
        const c = integer(b.column) && b.column >= 0 && b.column <= 7;
        const r = integer(b.row) && b.row >= 0 && b.row <= 7 && (!incoming || b.row === 0);
        const w = b.width === 1 || b.width === 2;
        if (!c) fail(`${p}.column`, '列号必须为 0–7');
        if (!r) fail(`${p}.row`, incoming ? '补行中的 row 必须为 0' : '行号必须为 0–7');
        if (!w) fail(`${p}.width`, '宽度只能为 1 或 2');
        if (c && w && b.column + b.width > 8) fail(p, '方块超出右边界');
        if (!integer(b.health) || b.health <= 0) fail(`${p}.health`, '血量必须为正整数');
        if (typeof b.target !== 'boolean') fail(`${p}.target`, '必须为 true 或 false');
        if (b.target === true) { targets++; if (!incoming || !last) fail(`${p}.target`, '目标必须位于最后一条补行'); }
        if (c && r && w && b.column + b.width <= 8) {
          for (let x=b.column; x<b.column+b.width; x++) {
            const cell = `${x},${b.row}`;
            if (occupied.has(cell)) fail(p, `与其他块重叠，列 ${x}，行 ${b.row}`);
            occupied.add(cell);
          }
        }
      });
    }
    blocks(level.initialBlocks, 'level.json.initialBlocks', false, false);
    if (!Array.isArray(level.incomingRows) || level.incomingRows.length === 0) fail('level.json.incomingRows', '至少需要一条补行');
    else level.incomingRows.forEach((row, i) => {
      const path = `level.json.incomingRows[${i}]`;
      if (!object(row)) { fail(path, '需要包含 blocks 的对象'); return; }
      keys(row, ['blocks'], path);
      blocks(row.blocks, `${path}.blocks`, true, i === level.incomingRows.length - 1);
    });
    if (targets !== 1) fail('level.json', `整个关卡必须有且仅有一个目标，当前 ${targets} 个`);
    return errors;
  }
  function placementError(blocks, block, selectedIndex, incoming) {
    if (!integer(block.column) || block.column<0 || block.column>7) return '列号必须为 0–7';
    if (!integer(block.row) || block.row<0 || block.row>7 || (incoming && block.row!==0)) return incoming ? '补行必须使用第 0 行' : '行号必须为 0–7';
    if (![1,2].includes(block.width) || block.column+block.width>8) return '宽度必须为 1 或 2，且不能越过第 7 列';
    if (!integer(block.health) || block.health<=0) return '血量必须为正整数';
    if (blocks.some((b,i) => i!==selectedIndex && b.row===block.row && b.column<block.column+block.width && b.column+b.width>block.column)) return '此位置与其他块重叠';
    return null;
  }
  return {clone, validate, placementError};
});
