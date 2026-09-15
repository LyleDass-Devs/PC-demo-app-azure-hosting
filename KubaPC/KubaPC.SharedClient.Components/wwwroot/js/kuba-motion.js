// KubaPC motion. Blazor owns the DOM; this module only animates elements it is handed,
// by recipe name, and forgets them when told. A WeakMap holds the live tweens so an
// element Blazor removes mid-animation is collected together with its tween.
(function () {
  const live = new WeakMap();
  const reduced = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  function ready() {
    if (!window.gsap) return false;
    if (window.ScrollTrigger && !gsap.core.globals().ScrollTrigger) gsap.registerPlugin(ScrollTrigger);
    return true;
  }

  const recipes = {
    "entry-tile": (el) => gsap.from(el, { y: 28, opacity: 0, duration: 0.42, ease: "expo.out" }),
    "mark-draw": (el) => gsap.fromTo(el.querySelectorAll("path"), { strokeDashoffset: 1 }, { strokeDashoffset: 0, duration: 0.9, ease: "power2.inOut", stagger: 0.15 }),
    "tile-in": (el) => gsap.from(el, { y: 16, opacity: 0, duration: 0.42, ease: "expo.out" }),
    // The one place height animates: a row leaving a list. Documented exception to transform-only.
    "tile-out": (el) => gsap.to(el, { height: 0, opacity: 0, paddingTop: 0, paddingBottom: 0, marginBottom: 0, duration: 0.3, ease: "expo.in" }),
    "board-reveal": (el) => {
      const tiles = el.querySelectorAll("[data-reveal]");
      gsap.set(tiles, { opacity: 0, y: 18 });
      return ScrollTrigger.batch(tiles, {
        start: "top 92%",
        once: true,
        onEnter: (batch) => gsap.to(batch, { opacity: 1, y: 0, duration: 0.42, ease: "expo.out", stagger: 0.06, overwrite: true })
      });
    }
  };

  window.kubaMotion = {
    // A Blazor app starts with one history entry; anything beyond it was our own navigation.
    canGoBack() { return window.history.length > 1 && document.referrer !== "" || window.history.length > 2; },
    animate(el, name) {
      const recipe = recipes[name];
      if (!el || !recipe || !ready()) return;
      if (reduced()) {
        if (name === "board-reveal") gsap.set(el.querySelectorAll("[data-reveal]"), { clearProps: "all" });
        if (name === "tile-out") el.style.display = "none";
        return;
      }
      this.dispose(el);
      live.set(el, recipe(el));
    },
    dispose(el) {
      const tween = el && live.get(el);
      if (!tween) return;
      (Array.isArray(tween) ? tween : [tween]).forEach((t) => t && t.kill && t.kill());
      live.delete(el);
    }
  };
})();
