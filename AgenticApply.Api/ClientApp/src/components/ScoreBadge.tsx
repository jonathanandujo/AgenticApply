function scoreTone(score: number): 'green' | 'blue' | 'muted' {
  if (score >= 90) return 'green'
  if (score >= 80) return 'blue'
  return 'muted'
}

export function ScoreBadge({ score }: { score: number }) {
  return (
    <span className={`score-badge score-${scoreTone(score)}`}>
      <span className="score-dot" />
      {score}
    </span>
  )
}
