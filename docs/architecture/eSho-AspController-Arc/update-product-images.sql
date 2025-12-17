-- CatalogDb Products tablosundaki ürünlerin ImageUrl'lerini güncelleme sorguları
-- pgAdmin'den CatalogDb veritabanında çalıştırın

-- Elektronik kategorisi ürünleri
UPDATE "Products"
SET "ImageUrl" = 'https://images.pexels.com/photos/18525574/pexels-photo-18525574.jpeg'
WHERE "Name" = 'iPhone 15';

UPDATE "Products"
SET "ImageUrl" = 'https://images.pexels.com/photos/15493878/pexels-photo-15493878.jpeg'
WHERE "Name" = 'Samsung Galaxy S24';

UPDATE "Products"
SET "ImageUrl" = 'https://images.pexels.com/photos/249538/pexels-photo-249538.jpeg'
WHERE "Name" = 'MacBook Pro';

-- Giyim kategorisi ürünleri
UPDATE "Products"
SET "ImageUrl" = 'https://images.pexels.com/photos/35203728/pexels-photo-35203728.jpeg'
WHERE "Name" = 'Beyaz T-Shirt';

UPDATE "Products"
SET "ImageUrl" = 'https://images.pexels.com/photos/10360630/pexels-photo-10360630.jpeg'
WHERE "Name" = 'Siyah Pantolon';

UPDATE "Products"
SET "ImageUrl" = 'https://images.pexels.com/photos/2996261/pexels-photo-2996261.jpeg'
WHERE "Name" = 'Spor Ayakkabı';

-- Ev & Yaşam kategorisi ürünleri
UPDATE "Products"
SET "ImageUrl" = 'https://images.pexels.com/photos/4348395/pexels-photo-4348395.jpeg'
WHERE "Name" = 'Ofis Masası';

UPDATE "Products"
SET "ImageUrl" = 'https://images.pexels.com/photos/116910/pexels-photo-116910.jpeg'
WHERE "Name" = 'Rahat Sandalye';

UPDATE "Products"
SET "ImageUrl" = 'https://images.pexels.com/photos/7439754/pexels-photo-7439754.jpeg'
WHERE "Name" = 'LED Lamba';

-- Güncelleme sonucunu kontrol etme
SELECT "Id", "Name", "ImageUrl"
FROM "Products"
ORDER BY "Name";

