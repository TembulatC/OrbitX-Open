// Цвет основного спутника
export const MAIN_SAT_COLOR = '#ea75a2'

// Оттенок основного спутника (#ea75a2 ≈ hue 338)
const MAIN_SAT_HUE = 338

// Кратчайшее расстояние между двумя оттенками на цветовом круге (0–180°)
const hueDistance = (a: number, b: number): number => {
  const d = Math.abs(a - b) % 360
  return d > 180 ? 360 - d : d
}

const hueOf = (color: string): number => {
  const m = /hsl\(\s*(\d+(?:\.\d+)?)/.exec(color)
  return m && m[1] !== undefined ? parseFloat(m[1]) : MAIN_SAT_HUE
}


// Выбор случайного цвета. Из всех кандидатов берётся тот, у которого расстояние до ближайшего занятого оттенка максимально.
export const pickDistinctColor = (usedColors: string[]): string => {
  const usedHues = [MAIN_SAT_HUE, ...usedColors.map(hueOf)]

  let bestHue = 0
  let bestDist = -1
  for (let h = 0; h < 360; h += 2) {
    const d = Math.min(...usedHues.map((u) => hueDistance(h, u)))
    // Небольшой случайный разброс, чтобы при равных расстояниях цвета не повторялись по шаблону
    if (d + Math.random() * 0.5 > bestDist) {
      bestDist = d
      bestHue = h
    }
  }

  const sat = 75 + Math.floor(Math.random() * 26)
  const lig = 52 + Math.floor(Math.random() * 14)
  return `hsl(${bestHue}, ${sat}%, ${lig}%)`
}
