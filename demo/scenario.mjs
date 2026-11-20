// Scripted walkthrough for the README demo. Run: node demo/record.mjs .
import { fileURLToPath } from "node:url";
const photo = fileURLToPath(new URL("./fixtures/harbour-dawn.jpg", import.meta.url));

// Count GPS tags (EXIF IFD pointer 0x8825) in a JPEG fetched by the page.
const gpsTagsIn = (page, url) => page.evaluate(async u => {
  const b = new Uint8Array(await (await fetch(u)).arrayBuffer());
  let hits = 0;
  for (let i = 0; i < b.length - 1; i++) if ((b[i] === 0x88 && b[i + 1] === 0x25) || (b[i] === 0x25 && b[i + 1] === 0x88)) hits++;
  const hasExif = new TextDecoder("latin1").decode(b.slice(0, 4096)).includes("Exif");
  return hasExif ? hits : 0;
}, url);

const viewAs = (page, name) => page.getByLabel("Viewing as (demo)").selectOption({ label: name });

export default async ({ page, step, shot }) => {
  await step("Framely: a quiet, photo-first home feed of the photographers you follow", async () => {
    await page.getByRole("article").first().waitFor();
  });
  await shot("feed");
  await step("Naledi posts a new frame. Her JPEG still carries GPS coordinates in its EXIF data", async () => {
    await viewAs(page, "Naledi Dube");
    await page.getByRole("button", { name: /Post a photo/ }).first().click();
    await page.getByLabel("Photo (JPEG)").setInputFiles(photo);
    await page.getByLabel("Caption").fill("Harbour at dawn, before the first boats go out.");
    await page.getByLabel("Describe the photo (alt text)").fill("Fishing boats on calm water under an orange sunrise");
  });
  await shot("compose");
  await step("On upload the server re-encodes the photo and strips its location data (POPIA)", async () => {
    await page.getByRole("button", { name: "Post to followers" }).click();
    await page.getByText(/Posted\./).waitFor();
  });
  let src = "";
  await step("Her follower Thandi opens Framely: the new frame is already first in her feed", async () => {
    await page.getByRole("dialog").waitFor({ state: "detached" });
    await viewAs(page, "Thandi Mahlangu");
    const card = page.getByRole("article").filter({ hasText: "Harbour at dawn" }).first();
    await card.waitFor();
    src = await card.getByRole("img").getAttribute("src");
  });
  await shot("follower-sees-it");
  const gps = await gpsTagsIn(page, src);
  await step(`Proof: the published copy has ${gps} GPS tags in its EXIF (the original had a full fix)`, async () => {
    await page.getByRole("article").first().scrollIntoViewIfNeeded();
  });
  await step("Kagiso doesn't follow Naledi, so the frame isn't in his feed: fan-out goes to followers only", async () => {
    await viewAs(page, "Kagiso Sello");
    await page.getByRole("article").first().waitFor();
  });
};

export async function mobile({ page, shot }) {
  await page.getByRole("article").first().waitFor();
  await page.waitForTimeout(800);
  await shot("mobile");
}
