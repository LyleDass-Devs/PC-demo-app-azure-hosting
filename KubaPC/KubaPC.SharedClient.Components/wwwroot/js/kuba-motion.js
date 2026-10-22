(function () {
  const live = new WeakMap();
  const marked = new WeakSet();
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
    revealSelected(selector) {
      const rail = document.querySelector(selector);
      const item = rail && rail.querySelector('[aria-selected="true"]');
      if (item) item.scrollIntoView({ block: "nearest", inline: "center", behavior: reduced() ? "auto" : "smooth" });
      this.markSelected(selector);
    },
    markSelected(selector) {
      const rail = document.querySelector(selector);
      const item = rail && rail.querySelector('[aria-selected="true"]');
      if (!rail || !item) return;
      const place = () => {
        rail.style.setProperty("--marker-x", item.offsetLeft + "px");
        rail.style.setProperty("--marker-w", item.offsetWidth + "px");
        rail.style.setProperty("--marker-shown", "1");
      };
      place();
      requestAnimationFrame(place);
      if (!marked.has(rail)) {
        marked.add(rail);
        new ResizeObserver(() => {
          const current = rail.querySelector('[aria-selected="true"]');
          if (!current) return;
          rail.style.setProperty("--marker-x", current.offsetLeft + "px");
          rail.style.setProperty("--marker-w", current.offsetWidth + "px");
        }).observe(rail);
      }
    },
    watchOutside(root, dotnet) {
      const onDown = (e) => {
        if (root.contains(e.target)) return;
        document.removeEventListener("pointerdown", onDown, true);
        dotnet.invokeMethodAsync("OutsideClick");
      };
      setTimeout(() => document.addEventListener("pointerdown", onDown, true), 0);
    },
    guardKeys(el, keys) {
      if (el) el.addEventListener("keydown", (e) => { if (keys.includes(e.key)) e.preventDefault(); });
    },
    scrollToId(id) {
      const el = document.getElementById(id);
      if (el) el.scrollIntoView({ block: "nearest" });
    },
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
