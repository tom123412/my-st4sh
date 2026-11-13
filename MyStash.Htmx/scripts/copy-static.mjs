import { copyFile, mkdir } from "node:fs/promises";

await mkdir(new URL("../dist/", import.meta.url), { recursive: true });
await Promise.all([
    copyFile(new URL("../index.html", import.meta.url), new URL("../dist/index.html", import.meta.url)),
    copyFile(new URL("../transactions.html", import.meta.url), new URL("../dist/transactions.html", import.meta.url)),
    copyFile(
        new URL("../staticwebapp.config.json", import.meta.url),
        new URL("../dist/staticwebapp.config.json", import.meta.url),
    ),
]);
