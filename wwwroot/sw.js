const CACHE_NAME = "taekwondo-juez-v1";

const ARCHIVOS = [
    "./juez.html",
    "./juez.css",
    "./juez.js",
    "./manifest.json"
];


self.addEventListener("install", function (event) {

    event.waitUntil(

        caches.open(CACHE_NAME)
            .then(function (cache) {

                return cache.addAll(ARCHIVOS);

            })
    );

    self.skipWaiting();
});


self.addEventListener("activate", function (event) {

    event.waitUntil(

        caches.keys().then(function (nombres) {

            return Promise.all(

                nombres.map(function (nombre) {

                    if (nombre !== CACHE_NAME) {

                        return caches.delete(nombre);

                    }

                    return Promise.resolve();
                })

            );

        })

    );

    self.clients.claim();
});


self.addEventListener("fetch", function (event) {

    event.respondWith(

        fetch(event.request)
            .catch(function () {

                return caches.match(event.request);

            })

    );

});