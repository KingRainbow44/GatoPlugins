import { Glob } from "bun";
import { readdir } from "node:fs/promises";

const releaseMode = (process.env["RELEASE_MODE"] || "true") == "true";
const otherDir = process.env["FINAL_COPY"] || undefined;
const otherFile = process.env["OTHER_FILE"] || undefined;

const plugins = new Glob("*");
for await (const plugin of plugins.scan({ onlyFiles: false })) {
    try {
        await readdir(plugin);

        const artifact = Bun.file(`${plugin}/bin/${releaseMode ? "Release" : "Debug"}/net8.0/${plugin}.dll`);
        if (!await artifact.exists()) continue;

        // Copy to `dist/artifact` directory.
        await Bun.write(`dist/${plugin}.dll`, artifact);

        if (otherDir) {
            // Copy to `dist/otherDir/artifact` directory.
            await Bun.write(`${otherDir}/${plugin}.dll`, artifact);
        }
    } catch {

    }
}

await (async () => {
    if (otherFile) {
        const files = otherFile.split(",");
        for (const file of files) {
            const artifact = Bun.file(file);
            if (!await artifact.exists()) return;

            const split = file.split("/");
            const fileName = split[split.length - 1];

            // Copy to `dist/artifact` directory.
            await Bun.write(`dist/${fileName}`, artifact);

            if (otherDir) {
                // Copy to `dist/otherDir/artifact` directory.
                await Bun.write(`${otherDir}/${fileName}`, artifact);
            }
        }
    }
})();
