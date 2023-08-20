export const classNames = (props: Record<string, any>): string =>
  Object.keys(props)
    .filter((f) => props[f] === true)
    .join(' ');
