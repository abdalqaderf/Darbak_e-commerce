// Darbak global client-side behaviors.

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initializeToasts();
        initializeProductGallery();
        initializeMobileNavigation();
        initializeHeaderSearch();
        initializeConfirmationDialog();
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
