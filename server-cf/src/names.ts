// Player names are generated, never typed, so there is nothing offensive to moderate.
// A name is an adjective + a noun + a number from 1 to 999, e.g. "NeonRook42" (about 1 million names).
// Names are handed out by the Accounts object, which keeps each one unique.

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
  const adjective = pick(ADJECTIVES);
  let noun = pick(NOUNS);
  while (noun === adjective) noun = pick(NOUNS); // both lists have "Rocket"
  return `${adjective}${noun}${1 + Math.floor(Math.random() * 999)}`;
}

/** True only for names the generator could have made */
export function isGeneratedName(name: string): boolean {
  const m = /^([A-Z][a-z]+)([A-Z][a-z]+)([1-9][0-9]{0,2})$/.exec(name);
  return !!m && ADJECTIVES.includes(m[1]) && NOUNS.includes(m[2]);
}
