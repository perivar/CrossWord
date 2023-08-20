/*  Auto scrolling with easing

    Usage:
    - scroller.scrollToElement(element, 500, 'easeOutQuad'); // 500ms scroll to element using easeOutQuad easing
    - scroller.scrollTo(1250, 250, 'linear'); // 250ms scroll to 1250px using linear gradient
    - scroller.scrollTo(100, 250, 'linear', document.querySelector('.container')); // 250ms scroll to 100px of scrollable container

    Note: if you pass in an element, you must also specify an easing function.
*/

import fastdom from 'fastdom';
import { createEasing } from './easing';

const scrollTo = (offset: number, duration = 0, easeFn = 'easeOutQuad', container = document.body) => {
  const $container = container;
  const from = $container.scrollTop;
  const distance = offset - from;
  const ease = createEasing(easeFn, duration);

  const scrollFn = () => {
    fastdom.mutate(() => $container.scroll(0, from + ease() * distance));
  };

  const interval = setInterval(scrollFn, 15);

  setTimeout(() => {
    clearInterval(interval);
    fastdom.mutate(() => $container.scroll(0, offset));
  }, duration);
};

// eslint-disable-next-line default-param-last
const scrollToElement = (element: HTMLElement, duration = 0, easeFn: string, container: HTMLElement) => {
  const top = element.offsetTop;
  scrollTo(top, duration, easeFn, container);
};

export { scrollTo, scrollToElement };
