// Darbak global client-side behaviors.

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initializeToasts();
        initializeProductGallery();
        initializeMobileNavigation();
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
})();
