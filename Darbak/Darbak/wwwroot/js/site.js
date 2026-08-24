// Darbak global client-side behaviors.

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initializeToasts();
        initializeProductGallery();
        initializeMobileNavigation();
        initializeHeaderSearch();
        initializeConfirmationDialog();
        initializeHeroVideo();
        initializeTestimonialCarousel();
        initializeTestimonialRating();
    });

    function initializeToasts() {
        if (typeof bootstrap === "undefined" || !bootstrap.Toast) {
            return;
        }

        document.querySelectorAll(".darbak-toast").forEach(function (toastElement) {
            const toast = bootstrap.Toast.getOrCreateInstance(toastElement);
            toast.show();
        });
    }

    function initializeProductGallery() {
        const mainProductImage = document.getElementById("product-main-image");
        const productThumbnails = document.querySelectorAll("[data-product-thumbnail]");

        if (!mainProductImage || productThumbnails.length === 0) {
            return;
        }

        productThumbnails.forEach(function (thumbnail) {
            thumbnail.addEventListener("click", function () {
                const imageUrl = thumbnail.getAttribute("data-image-url");
                const imageAlt = thumbnail.getAttribute("data-image-alt");

                if (!imageUrl) {
                    return;
                }

                mainProductImage.src = imageUrl;

                if (imageAlt) {
                    mainProductImage.alt = imageAlt;
                }

                productThumbnails.forEach(function (item) {
                    item.classList.remove("is-active");
                    item.setAttribute("aria-pressed", "false");
                });

                thumbnail.classList.add("is-active");
                thumbnail.setAttribute("aria-pressed", "true");
            });
        });
    }

    function initializeMobileNavigation() {
        if (typeof bootstrap === "undefined") {
            return;
        }

        const siteNavigation = document.getElementById("siteNavigation");

        if (siteNavigation && bootstrap.Collapse) {
            siteNavigation.querySelectorAll("a.nav-link").forEach(function (link) {
                link.addEventListener("click", function () {
                    if (window.matchMedia("(max-width: 991.98px)").matches && siteNavigation.classList.contains("show")) {
                        const collapse = bootstrap.Collapse.getOrCreateInstance(siteNavigation, { toggle: false });
                        collapse.hide();
                    }
                });
            });
        }

        const adminSidebar = document.getElementById("adminSidebar");

        if (adminSidebar && bootstrap.Offcanvas) {
            adminSidebar.querySelectorAll("a.admin-nav__link").forEach(function (link) {
                link.addEventListener("click", function () {
                    if (window.matchMedia("(max-width: 991.98px)").matches) {
                        const offcanvas = bootstrap.Offcanvas.getInstance(adminSidebar);
                        if (offcanvas) {
                            offcanvas.hide();
                        }
                    }
                });
            });
        }
    }

    function initializeHeaderSearch() {
        const trigger = document.querySelector("[data-site-search-trigger]");
        const panel = document.querySelector("[data-site-search-panel]");

        if (!trigger || !panel) {
            return;
        }

        const input = panel.querySelector("input[type='search']");

        function openSearch() {
            panel.classList.add("is-open");
            trigger.classList.add("is-active");
            trigger.setAttribute("aria-expanded", "true");
            trigger.setAttribute("aria-label", "Close search");

            window.setTimeout(function () {
                if (input) {
                    input.focus();
                    input.select();
                }
            }, 40);
        }

        function closeSearch() {
            panel.classList.remove("is-open");
            trigger.classList.remove("is-active");
            trigger.setAttribute("aria-expanded", "false");
            trigger.setAttribute("aria-label", "Open search");
        }

        trigger.addEventListener("click", function () {
            if (panel.classList.contains("is-open")) {
                closeSearch();
            } else {
                openSearch();
            }
        });

        document.addEventListener("click", function (event) {
            if (!panel.classList.contains("is-open")) {
                return;
            }

            if (!panel.contains(event.target) && !trigger.contains(event.target)) {
                closeSearch();
            }
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && panel.classList.contains("is-open")) {
                closeSearch();
                trigger.focus();
            }
        });
    }


    function initializeHeroVideo() {
        const video = document.querySelector("[data-hero-video]");

        if (!video) {
            return;
        }

        const source = video.dataset.src;
        const desktopViewport = window.matchMedia("(min-width: 768px)");
        const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
        let sourceAttached = false;

        function enableVideo() {
            if (!source || sourceAttached) {
                return;
            }

            video.src = source;
            sourceAttached = true;
            video.load();

            const playPromise = video.play();
            if (playPromise && typeof playPromise.catch === "function") {
                playPromise.catch(function () {
                    // The static hero image remains visible if autoplay is blocked.
                });
            }
        }

        function disableVideo() {
            if (!sourceAttached) {
                return;
            }

            video.pause();
            video.classList.remove("is-ready");
            video.removeAttribute("src");
            video.load();
            sourceAttached = false;
        }

        function syncVideoState() {
            if (desktopViewport.matches && !reducedMotion.matches) {
                enableVideo();
            } else {
                disableVideo();
            }
        }

        video.addEventListener("canplay", function () {
            video.classList.add("is-ready");
        });

        video.addEventListener("error", function () {
            video.classList.remove("is-ready");
        });

        document.addEventListener("visibilitychange", function () {
            if (!sourceAttached) {
                return;
            }

            if (document.hidden) {
                video.pause();
                return;
            }

            const playPromise = video.play();
            if (playPromise && typeof playPromise.catch === "function") {
                playPromise.catch(function () { });
            }
        });

        if (typeof desktopViewport.addEventListener === "function") {
            desktopViewport.addEventListener("change", syncVideoState);
            reducedMotion.addEventListener("change", syncVideoState);
        } else {
            desktopViewport.addListener(syncVideoState);
            reducedMotion.addListener(syncVideoState);
        }

        syncVideoState();
    }


    function initializeTestimonialCarousel() {
        document.querySelectorAll("[data-testimonial-carousel]").forEach(function (rail) {
            const targetId = rail.id;
            if (!targetId) {
                return;
            }

            const buttons = Array.from(document.querySelectorAll(`[data-testimonial-target="${targetId}"]`));
            if (buttons.length === 0) {
                return;
            }

            function updateButtons() {
                const maxScroll = Math.max(0, rail.scrollWidth - rail.clientWidth);
                buttons.forEach(function (button) {
                    const direction = Number(button.dataset.testimonialScroll || "0");
                    if (direction < 0) {
                        button.disabled = rail.scrollLeft <= 4;
                    } else if (direction > 0) {
                        button.disabled = rail.scrollLeft >= maxScroll - 4;
                    }
                });
            }

            buttons.forEach(function (button) {
                button.addEventListener("click", function () {
                    const direction = Number(button.dataset.testimonialScroll || "0");
                    if (!direction) {
                        return;
                    }

                    const firstCard = rail.querySelector(".home-testimonial-slide");
                    const styles = window.getComputedStyle(rail);
                    const gap = parseFloat(styles.columnGap || styles.gap || "0") || 0;
                    const distance = firstCard
                        ? firstCard.getBoundingClientRect().width + gap
                        : rail.clientWidth * 0.82;

                    rail.scrollBy({
                        left: direction * distance,
                        behavior: "smooth"
                    });
                });
            });

            rail.addEventListener("scroll", updateButtons, { passive: true });
            window.addEventListener("resize", updateButtons);
            updateButtons();
        });
    }

    function initializeTestimonialRating() {
        document.querySelectorAll("[data-testimonial-rating-input]").forEach(function (group) {
            const radios = Array.from(group.querySelectorAll("input[type='radio']"));
            const labels = Array.from(group.querySelectorAll("[data-rating-value]"));

            if (radios.length === 0 || labels.length === 0) {
                return;
            }

            function paint(value) {
                labels.forEach(function (label) {
                    const starValue = Number(label.dataset.ratingValue || "0");
                    label.classList.toggle("is-filled", starValue <= value);
                });
            }

            function selectedValue() {
                const checked = radios.find(function (radio) {
                    return radio.checked;
                });

                return checked ? Number(checked.value) : 0;
            }

            radios.forEach(function (radio) {
                radio.addEventListener("change", function () {
                    paint(selectedValue());
                });
            });

            labels.forEach(function (label) {
                label.addEventListener("mouseenter", function () {
                    paint(Number(label.dataset.ratingValue || "0"));
                });
            });

            group.addEventListener("mouseleave", function () {
                paint(selectedValue());
            });

            paint(selectedValue());
        });
    }


    function initializeConfirmationDialog() {
        const dialogElement = document.getElementById("darbakConfirmDialog");

        if (!dialogElement || typeof bootstrap === "undefined" || !bootstrap.Modal) {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(dialogElement, {
            backdrop: "static",
            keyboard: true,
            focus: true
        });
        const titleElement = dialogElement.querySelector("#darbakConfirmTitle");
        const messageElement = dialogElement.querySelector("#darbakConfirmMessage");
        const acceptButton = dialogElement.querySelector("[data-confirm-accept]");

        let pendingForm = null;
        let pendingSubmitter = null;
        let sourceElement = null;

        function resetDialog() {
            if (titleElement) {
                titleElement.textContent = "Are you sure?";
            }

            if (messageElement) {
                messageElement.textContent = "Please confirm this action.";
            }

            if (acceptButton) {
                acceptButton.textContent = "Confirm";
            }
        }

        document.addEventListener("submit", function (event) {
            const form = event.target.closest("form[data-confirm]");

            if (!form || form.dataset.confirmed === "true") {
                if (form && form.dataset.confirmed === "true") {
                    delete form.dataset.confirmed;
                }
                return;
            }

            event.preventDefault();

            pendingForm = form;
            pendingSubmitter = event.submitter || null;
            sourceElement = pendingSubmitter || form;

            if (titleElement) {
                titleElement.textContent = form.dataset.confirmTitle || "Are you sure?";
            }

            if (messageElement) {
                messageElement.textContent = form.dataset.confirmMessage || "Please confirm this action.";
            }

            if (acceptButton) {
                acceptButton.textContent = form.dataset.confirmButton || "Confirm";
            }

            modal.show();
        });

        if (acceptButton) {
            acceptButton.addEventListener("click", function () {
                if (!pendingForm) {
                    modal.hide();
                    return;
                }

                const formToSubmit = pendingForm;
                const submitter = pendingSubmitter;

                pendingForm = null;
                pendingSubmitter = null;
                formToSubmit.dataset.confirmed = "true";
                modal.hide();

                window.setTimeout(function () {
                    if (typeof formToSubmit.requestSubmit === "function") {
                        if (submitter && submitter.form === formToSubmit) {
                            formToSubmit.requestSubmit(submitter);
                        } else {
                            formToSubmit.requestSubmit();
                        }
                    } else {
                        formToSubmit.submit();
                    }
                }, 80);
            });
        }

        dialogElement.addEventListener("hidden.bs.modal", function () {
            pendingForm = null;
            pendingSubmitter = null;
            resetDialog();

            if (sourceElement && typeof sourceElement.focus === "function") {
                sourceElement.focus();
            }

            sourceElement = null;
        });
    }

})();
