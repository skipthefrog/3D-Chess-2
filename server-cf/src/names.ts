// Player names are generated, never typed, so there is nothing offensive to moderate.
// A name is an adjective + a noun + a number from 1 to 99, e.g. "NeonRook42".
// Keep these lists in sync with Assets/Scripts/Network/Online/PlayerNames.cs.

export const ADJECTIVES = [
  'Neon', 'Turbo', 'Glitchy', 'Cosmic', 'Pixel', 'Laser', 'Hyper', 'Chrome',
  'Electric', 'Lucky', 'Sneaky', 'Fuzzy', 'Zippy', 'Mega', 'Retro', 'Wobbly',
  'Sparkly', 'Funky', 'Jolly', 'Brave', 'Swift', 'Quantum', 'Rocket', 'Disco',
  'Shiny', 'Bouncy', 'Mighty', 'Clever', 'Groovy', 'Stellar', 'Atomic', 'Sunny',
];

export const NOUNS = [
  'Rook', 'Knight', 'Bishop', 'Pawn', 'Queen', 'King', 'Castle', 'Gambit',
  'Robot', 'Comet', 'Panda', 'Otter', 'Falcon', 'Taco', 'Waffle', 'Pickle',
  'Noodle', 'Nova', 'Dragon', 'Frog', 'Llama', 'Yeti', 'Wizard', 'Penguin',
  'Koala', 'Rocket', 'Meteor', 'Cactus', 'Donut', 'Narwhal', 'Phoenix', 'Gecko',
];

export function randomName(): string {
  const pick = <T>(a: T[]) => a[Math.floor(Math.random() * a.length)];
  return `${pick(ADJECTIVES)}${pick(NOUNS)}${1 + Math.floor(Math.random() * 99)}`;
}

/** True only for names the generator could have made */
export function isGeneratedName(name: string): boolean {
  const m = /^([A-Z][a-z]+)([A-Z][a-z]+)([1-9][0-9]?)$/.exec(name);
  return !!m && ADJECTIVES.includes(m[1]) && NOUNS.includes(m[2]);
}
