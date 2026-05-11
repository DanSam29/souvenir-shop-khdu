# Опис структур даних (DTO, entities)
## Вступ
Даний документ містить детальний опис структур даних, які використовуються в інформаційній системі інтернет-магазину сувенірної продукції ХДУ. Документ охоплює дві основні категорії структур:  
  - Entities (Domain Models) - сутності предметної області, що відображають структуру бази даних  
  - DTOs (Data Transfer Objects) - об'єкти передачі даних для комунікації між шарами додатку
## Domain Models (Entities)
### 1. User (Користувач)
Призначення: Зберігає інформацію про користувачів системи (гості, клієнти, менеджери, адміністратори).  
Таблиця в БД: Users  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ, auto-increment |
| firstName | string(50) | Так | Ім'я користувача |
| lastName | string(50) | Так | Прізвище користувача |
| email | string(100) | Так | Email (унікальний) |
| passwordHash | string(255) | Так | Хешований пароль (bcrypt) |
| phone | string(20) | Ні | Телефон користувача |
| role | Role | Так | Роль в системі (enum) |
| status | UserStatus | Так | Статус облікового запису (enum) |
| studentStatus | StudentStatus | Так | Статус студента (enum, default: NONE) |
| gpa | decimal(3,2) | Ні | Середній бал (0.00-5.00) |
| studentVerifiedAt | DateTime | Ні | Дата верифікації через University API |
| studentExpiresAt | DateTime | Ні | Дата закінчення студентського статусу |
| createdAt | DateTime | Так | Дата реєстрації |
| updatedAt | DateTime | Так | Дата останнього оновлення |

Методи:  
  - register() - реєстрація нового користувача  
  - login() - автентифікація та генерація JWT токену  
  - updateProfile() - оновлення профілю  
  - changePassword() - зміна паролю  
  - block() - блокування облікового запису  
  - unblock() - розблокування облікового запису  

Зв'язки:  
  - 1:1 з Cart (один кошик на користувача)  
  - 1:N з Order (багато замовлень)  
  - 1:N з Shipping (збережені адреси доставки)  
  - 1:N з UserPromotion (персональні знижки)  
  - 1:N з Promotion (створені менеджером, через createdBy)  
### 2. Product (Товар)
Призначення: Зберігає інформацію про товари магазину.  
Таблиця в БД: Products  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| name | string(200) | Так | Назва товару |
| description | text | Так | Опис товару |
| price | decimal(10,2) | Так | Ціна в гривнях |
| weight | decimal(10,3) | Так | Вага в кг (для розрахунку доставки) |
| categoryId | int | Так | FK до Categories |
| createdAt | DateTime | Так | Дата створення |
| updatedAt | DateTime | Так | Дата оновлення |

Обчислювані поля:  
  - stock - поточний залишок на складі (розраховується через WarehouseModule на основі IncomingDocuments та OutgoingDocuments)  

Методи:  
  - create() - створення товару  
  - update() - оновлення інформації  
  - delete() - видалення товару  
  - getStock() - отримання поточного залишку  
  - checkAvailability(quantity) - перевірка доступності кількості  

Зв'язки:  
  - N:1 з Category  
  - 1:N з ProductImage  
  - 1:N з CartItem  
  - 1:N з OrderItem  
  - 1:N з IncomingDocument  
  - 1:N з OutgoingDocument  
### 3. Category (Категорія)
Призначення: Організація товарів в ієрархічну структуру категорій.  
Таблиця в БД: Categories  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| name | string(100) | Так | Назва категорії |
| parentId | int | Ні | FK до батьківської категорії (self-reference) |
| order | int | Так | Порядок відображення (default: 0) |
| createdAt | DateTime | Так | Дата створення |

Методи:  
  - create() - створення категорії  
  - update() - оновлення категорії  
  - delete() - видалення категорії  
  - getChildren() - отримання дочірніх категорій  
  - getProducts() - отримання товарів категорії  

Зв'язки:  
  - 1:N з Product  
  - 0..1:N з Category (рекурсивний зв'язок для ієрархії)  
### 4. ProductImage (Зображення товару)
Призначення: Зберігання посилань на зображення товарів.  
Таблиця в БД: ProductImages  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| productId | int | Так | FK до Products |
| imageUrl | string(500) | Так | URL зображення в MinIO Storage |
| isPrimary | bool | Так | Головне зображення (default: false) |
| order | int | Так | Порядок відображення (default: 0) |

Методи:  
  - upload() - завантаження зображення в MinIO  
  - delete() - видалення зображення  
  - setPrimary() - встановлення головного зображення  

Зв'язки:  
  - N:1 з Product  
### 5. Cart (Кошик)
Призначення: Тимчасове зберігання товарів, обраних користувачем для покупки.  
Таблиця в БД: Cart  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| userId | int | Так | FK до Users (унікальний) |
| createdAt | DateTime | Так | Дата створення |
| updatedAt | DateTime | Так | Дата останнього оновлення |

Методи:  
  - addItem(productId, quantity) - додавання товару  
  - removeItem(productId) - видалення товару  
  - updateQuantity(productId, quantity) - оновлення кількості  
  - clear() - очищення кошика  
  - getTotal() - розрахунок загальної суми  

Зв'язки:  
  - 1:1 з User  
  - 1:N з CartItem (композиція)  
### 6. CartItem (Елемент кошика)
Призначення: Зберігання товарів в кошику з кількістю.  
Таблиця в БД: CartItems  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| cartId | int | Так | FK до Cart |
| productId | int | Так | FK до Products |
| quantity | int | Так | Кількість (default: 1) |
| addedAt | DateTime | Так | Дата додавання |

Унікальні обмеження:  
  - (cartId, productId) - один товар раз у кошику  

Методи:  
  - getSubtotal() - розрахунок вартості позиції (price × quantity)  

Зв'язки:  
  - N:1 з Cart  
  - N:1 з Product  
### 7. Order (Замовлення)
Призначення: Зберігання інформації про замовлення клієнтів.  
Таблиця в БД: Orders  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| orderNumber | string(50) | Так | Унікальний номер для клієнта |
| userId | int | Так | FK до Users |
| status | OrderStatus | Так | Статус замовлення (enum) |
| totalAmount | decimal(10,2) | Так | Загальна сума (товари + доставка) |
| shippingCost | decimal(10,2) | Так | Вартість доставки |
| paymentMethod | PaymentMethod | Так | Спосіб оплати (enum) |
| createdAt | DateTime | Так | Дата створення |
| updatedAt | DateTime | Так | Дата оновлення |

Методи:  
  - create() - створення замовлення  
  - updateStatus(newStatus) - зміна статусу  
  - cancel(reason) - скасування замовлення  
  - getTotal() - розрахунок загальної суми  

Зв'язки:  
  - N:1 з User  
  - 1:N з OrderItem (композиція, включаючи інформацію про знижки)  
  - 1:1 з Payment  
  - 1:1 з Shipping  
  - 1:N з OrderHistory  
  - 1:N з OutgoingDocument (копіює знижки з OrderItem)  
### 8. OrderItem (Товар в замовленні)
Призначення: Зберігання товарів в замовленні з фіксацією ціни.  
Таблиця в БД: OrderItems  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| orderId | int | Так | FK до Orders |
| productId | int | Так | FK до Products |
| quantity | int | Так | Кількість товару |
| originalPrice | decimal(10,2) | Так | Базова ціна товару (без знижок) |
| appliedPromotionId | int | Ні | FK до Promotions (найвигідніша знижка) |
| discountAmount | decimal(10,2) | Так | Сума знижки в грн (default: 0) |
| finalPrice | decimal(10,2) | Так | Фінальна ціна після знижки |

Методи:  
  - getSubtotal() - розрахунок вартості позиції (finalPrice × quantity)  
  - getDiscountTotal() - розрахунок загальної суми знижки (discountAmount × quantity)  
  - getOriginalTotal() - розрахунок вартості без знижок (originalPrice × quantity)  
 
Зв'язки:  
  - N:1 з Order  
  - N:1 з Product  
  - N:1 з Promotion (якщо appliedPromotionId не null)  
### 9. OrderHistory (Історія замовлення)
Призначення: Аудит всіх змін статусів замовлення.  
Таблиця в БД: OrderHistory  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| orderId | int | Так | FK до Orders |
| oldStatus | OrderStatus | Ні | Попередній статус |
| newStatus | OrderStatus | Так | Новий статус |
| changedBy | int | Ні | FK до Users (хто змінив) |
| comment | text | Ні | Коментар (наприклад, ТТН) |
| timestamp | DateTime | Так | Час зміни |

Методи:  
  - log() - логування зміни статусу  

Зв'язки:  
  - N:1 з Order  
  - N:1 з User  
### 10. Payment (Платіж)
Призначення: Зберігання інформації про платежі.  
Таблиця в БД: Payments  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| orderId | int | Так | FK до Orders (унікальний) |
| amount | decimal(10,2) | Так | Сума платежу |
| currency | string(3) | Так | Валюта (UAH) |
| method | PaymentMethod | Так | Спосіб оплати (enum) |
| status | PaymentStatus | Так | Статус платежу (enum) |
| stripeSessionId | string(255) | Ні | ID сесії Stripe |
| stripePaymentIntentId | string(255) | Ні | ID транзакції Stripe |
| createdAt | DateTime | Так | Дата створення |
| updatedAt | DateTime | Так | Дата оновлення |

Методи:  
  - createSession() - створення платіжної сесії Stripe  
  - updateStatus(status) - оновлення статусу  
  - refund() - повернення коштів  

Зв'язки:  
  - 1:1 з Order  
### 11. Shipping (Доставка)
Призначення: Інформація про доставку замовлення або збережена адреса користувача.  
Таблиця в БД: Shipping  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| orderId | int | Ні | FK до Orders (для доставки замовлення) |
| userId | int | Ні | FK до Users (для збереженої адреси) |
| city | string(100) | Так | Населений пункт |
| cityRef | string(100) | Так | Ref міста Nova Poshta |
| warehouse | string(100) | Так | Номер відділення |
| warehouseRef | string(100) | Так | Ref відділення Nova Poshta |
| phone | string(20) | Так | Контактний телефон |
| trackingNumber | string(100) | Ні | ТТН Nova Poshta |
| isDefault | bool | Так | Адреса за замовчуванням (default: false) |

Методи:  
  - save() - збереження адреси  
  - calculateCost() - розрахунок вартості доставки через Nova Poshta API  

Зв'язки:  
  - 1:1 з Order (якщо orderId не null)  
  - N:1 з User (якщо userId не null)  
### 12. IncomingDocument (Прибуткова накладна)
Призначення: Документування надходження товарів на склад.  
Таблиця в БД: IncomingDocuments  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| productId | int | Так | FK до Products |
| quantity | int | Так | Кількість надходження |
| purchasePrice | decimal(10,2) | Так | Закупівельна ціна за одиницю |
| companyId | int | Так | FK до Companies (фірма-постачальник) |
| documentDate | Date | Так | Дата документа |
| notes | text | Ні | Примітки |
| createdAt | DateTime | Так | Дата створення запису |
| createdBy | int | Так | FK до Users (менеджер) |

Методи:  
  - create() - створення накладної  
  - updateStock() - збільшення Stock товару  

Зв'язки:  
  - N:1 з Product  
  - N:1 з Company (постачальник)  
  - N:1 з User (createdBy)  
### 13. OutgoingDocument (Видаткова накладна)
Призначення: Документування списання товарів зі складу.  
Таблиця в БД: OutgoingDocuments  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ |
| productId | int | Так | FK до Products |
| quantity | int | Так | Кількість списання |
| orderId | int | Ні | FK до Orders (якщо списання через замовлення) |
| companyId | int | Умовно | FK до Companies (обов'язковий якщо reason=RETURN) |
| reason | OutgoingReason | Так | Причина списання (enum) |
| originalPrice | decimal(10,2) | Ні | Базова ціна товару (тільки для ORDER) |
| appliedPromotionId | int | Ні | FK до Promotions (тільки для ORDER) |
| discountAmount | decimal(10,2) | Ні | Сума знижки (тільки для ORDER) |
| finalPrice | decimal(10,2) | Ні | Фінальна ціна після знижки (ORDER) |
| notes | text | Ні | Примітки |
| documentDate | Date | Так | Дата документа |
| createdAt | DateTime | Так | Дата створення |
| createdBy | int | Ні | FK до Users (NULL для автоматичних) |

Правила валідації:  
  - companyId обов\'язковий, якщо reason = RETURN (повернення товару постачальнику)  
  - Для інших причин (DAMAGED, LOST, INVENTORY) companyId = NULL  
  - Поля знижок (originalPrice, appliedPromotionId, discountAmount, finalPrice) заповнюються ТІЛЬКИ для reason = ORDER  
  - Для reason = ORDER ці поля копіюються з відповідного OrderItem  
  - Для інших причин поля знижок = NULL  

Методи:  
  - create() - створення накладної  
  - updateStock() - зменшення Stock товару  

Зв'язки:  
  - N:1 з Product  
  - N:1 з Order (якщо orderId не null)  
  - N:1 з Company (якщо companyId не null - для повернень)  
  - N:1 з User (createdBy)  
  - N:1 з Promotion (якщо appliedPromotionId не null - для ORDER)  
### 14. Promotion (Акція/Знижка)
Призначення: Управління акціями та знижками на товари. Підтримує різні типи знижок, цільові аудиторії, промокоди, умови застосування та стакування знижок.  
Таблиця в БД: Promotions  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ, auto-increment |
| name | string(200) | Так | Назва акції |
| description | text | Ні | Опис акції |
| type | PromotionType | Так | Тип знижки (enum) |
| value | decimal(10,2) | Так | Значення знижки (%, грн або ціна) |
| targetType | PromotionTarget | Так | Область застосування (enum) |
| targetId | int | Ні | ID товару/категорії (nullable) |
| audienceType | PromotionAudience | Так | Цільова аудиторія (enum, default: ALL) |
| startDate | DateTime | Ні | Дата початку дії (nullable) |
| endDate | DateTime | Ні | Дата закінчення дії (nullable) |
| promoCode | string(50) | Ні | Промокод для активації (unique, nullable) |
| minOrderAmount | decimal(10,2) | Ні | Мінімальна сума замовлення (nullable) |
| minQuantity | int | Ні | Мінімальна кількість товару (nullable) |
| priority | int | Так | Пріоритет застосування (default: 0) |
| usageLimit | int | Ні | Ліміт використань (nullable) |
| currentUsage | int | Так | Поточна кількість використань (default: 0) |
| isActive | bool | Так | Чи активна акція (default: true) |
| createdBy | int | Так | FK до Users (менеджер) |
| createdAt | DateTime | Так | Дата створення |
| updatedAt | DateTime | Так | Дата оновлення |

Методи:  
  - create() - створення нової акції  
  - update() - оновлення параметрів акції  
  - activate() - активація акції (isActive = true)  
  - deactivate() - деактивація акції (isActive = false)  
  - delete() - видалення акції (м'яке видалення)  
  - validatePromoCode(code) - перевірка валідності промокоду  
  - checkConditions(orderAmount, quantity) - перевірка умов застосування  
  - incrementUsage() - збільшення лічильника використань  
  - isValid() - перевірка чи акція дійсна (активна, в межах дат, не вичерпано ліміт)  
  - calculateDiscount(originalPrice) - розрахунок суми знижки  

Зв'язки:  
  - N:1 з User (createdBy - менеджер який створив)  
  - 1:N з UserPromotion (персональні призначення)  
  - 1:N з OrderItem (застосовані знижки в замовленнях)  
  - 1:N з OutgoingDocument (застосовані знижки при списанні)  
  - N:1 з Product (якщо targetType=PRODUCT)  
  - N:1 з Category (якщо targetType=CATEGORY)  
### 15. UserPromotion (Персональна знижка)
Призначення: Зв'язок між користувачами та акціями (many-to-many). Зберігає інформацію про персональні знижки користувачів та історію їх використання.  
Таблиця в БД: UserPromotions  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ, auto-increment |
| userId | int | Так | FK до Users |
| promotionId | int | Так | FK до Promotions |
| assignedAt | DateTime | Так | Дата призначення знижки |
| usedCount | int | Так | Кількість використань (default: 0) |

Методи:  
  - assign(userId, promotionId) - призначення знижки користувачу  
  - incrementUsedCount() - збільшення лічильника використань  
  - getHistory(userId) - отримання історії використаних знижок  

Зв'язки:  
  - N:1 з User  
  - N:1 з Promotion  
### 16. Company (Фірма-постачальник)
Призначення: Зберігання інформації про фірми-постачальники для складського обліку.  
Таблиця в БД: Companies  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | Первинний ключ, auto-increment |
| name | string(200) | Так | Назва фірми (унікальна) |
| contactPerson | string(100) | Ні | Контактна особа |
| phone | string(20) | Ні | Телефон |
| email | string(100) | Ні | Email (унікальний) |
| address | string(500) | Ні | Адреса фірми |
| notes | text | Ні | Примітки |
| isActive | bool | Так | Чи активна фірма (default: true) |
| createdAt | DateTime | Так | Дата створення |
| updatedAt | DateTime | Так | Дата останнього оновлення |

Методи:  
  - create() - створення нової фірми  
  - update() - оновлення даних фірми  
  - deactivate() - деактивація фірми (м'яке видалення)  
  - getIncomingDocuments() - отримання всіх прибуткових накладних від цієї фірми  
  - getOutgoingDocuments() - отримання всіх повернень цій фірмі  

Зв'язки:  
  - 1:N з IncomingDocument (одна фірма може мати багато надходжень)  
  - 1:N з OutgoingDocument (одна фірма може мати багато повернень)
## Data Transfer Objects (DTOs)
### 1. RegisterDTO
Призначення: Передача даних реєстрації нового користувача.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| firstName | string | Так | 2-50 символів |
| lastName | string | Так | 2-50 символів |
| email | string | Так | Валідний email |
| password | string | Так | Мін. 8 символів, букви + цифри |
| confirmPassword | string | Так | Має співпадати з password |
| phone | string | Ні | Формат: +380XXXXXXXXX |
| acceptTerms | bool | Так | Має бути true |

Використовується в:  
  - AuthController.register()  
### 2. LoginDTO
Призначення: Передача даних для автентифікації.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| email | string | Так | Валідний email |
| password | string | Так | Не порожній |

Використовується в:  
  - AuthController.login()  
### 3. ProductDTO
Призначення: Передача даних товару (створення/оновлення).  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| name | string | Так | 3-200 символів |
| description | string | Так | 10-5000 символів |
| price | decimal | Так | > 0 |
| weight | decimal | Так | > 0, макс. 3 знаки після коми |
| categoryId | int | Так | Існуюча категорія |
| images | List<File> | Так (для створення) | JPEG/PNG/WebP, макс. 5MB |

Використовується в:  
  - ProductController.create()  
  - ProductController.update()  
### 4. CategoryDTO
Призначення: Передача даних категорії.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| name | string | Так | 3-100 символів |
| parentId | int | Ні | Існуюча категорія |
| order | int | Ні | >= 0 |

Використовується в:  
  - CategoryController.create()  
  - CategoryController.update()  
### 5. AddToCartDTO
Призначення: Додавання товару до кошика.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| productId | int | Так | Існуючий товар |
| quantity | int | Так | > 0 |

Використовується в:  
  - CartController.addItem()  
### 6. UpdateCartDTO
Призначення: Оновлення кількості товару в кошику.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| productId | int | Так | Існуючий товар в кошику |
| quantity | int | Так | > 0 |

Використовується в:  
  - CartController.updateItem()  
### 7. CreateOrderDTO
Призначення: Оформлення замовлення.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| items | List<OrderItemDTO> | Так | Не порожній |
| shippingAddress | AddressDTO | Так | Валідна адреса |
| paymentMethod | PaymentMethod | Так | Card або CashOnDelivery |

Вкладені DTO:  
  - OrderItemDTO: { productId: int, quantity: int }  

Використовується в:  
  - OrderController.createOrder()  
### 8. AddressDTO
Призначення: Передача даних адреси доставки.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| city | string | Так | Не порожній |
| cityRef | string | Так | Ref з Nova Poshta API |
| warehouse | string | Так | Номер відділення |
| warehouseRef | string | Так | Ref з Nova Poshta API |
| phone | string | Так | Формат: +380XXXXXXXXX |
| isDefault | bool | Ні | Default: false |

Використовується в:  
  - CreateOrderDTO  
  - ShippingController.addAddress()  
  - UserController.addAddress()  
### 9. UpdateProfileDTO
Призначення: Оновлення профілю користувача.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| firstName | string | Так | 2-50 символів |
| lastName | string | Так | 2-50 символів |
| phone | string | Ні | Формат: +380XXXXXXXXX |

Використовується в:  
  - UserController.updateProfile()  
### 10. ChangePasswordDTO
Призначення: Зміна паролю користувача.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| currentPassword | string | Так | Не порожній |
| newPassword | string | Так | Мін. 8 символів, букви + цифри |
| confirmPassword | string | Так | Має співпадати з newPassword |

Використовується в:  
  - UserController.changePassword()  
### 11. IncomingDTO
Призначення: Оформлення прибуткової накладної.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| productId | int | Так | Існуючий товар |
| quantity | int | Так | > 0 |
| purchasePrice | decimal | Так | > 0 |
| companyId | int | Так | Існуюча активна фірма |
| documentDate | Date | Так | <= поточна дата |
| notes | string | Ні | Макс. 1000 символів |

Використовується в:  
  - WarehouseController.createIncoming()  
### 12. OutgoingDTO
Призначення: Оформлення видаткової накладної (вручну).  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| productId | int | Так | Існуючий товар |
| quantity | int | Так | > 0, <= Stock |
| companyId | int | Умовно | Обов'язковий якщо reason=RETURN, інакше null |
| reason | OutgoingReason | Так | Damaged/Lost/Return/Inventory |
| notes | string | Ні | Макс. 1000 символів |

Примітка: Reason "Order" використовується тільки для автоматичних накладних при замовленні.  
Правило валідації: якщо reason = RETURN, то companyId обов'язковий та має відповідати активній фірмі в БД.  
Використовується в:  
  - WarehouseController.createOutgoing()  
### 13. CompanyDTO
Призначення: Базовий DTO для передачі даних фірми.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| name | string | Так | 3-200 символів, унікальна назва |
| contactPerson | string | Так | 3-100 символів |
| phone | string | Так | Формат: +380XXXXXXXXX |
| email | string | Так | Валідний email, унікальний |
| address | string | Так | Макс. 500 символів |
| notes | string | Ні | Макс. 1000 символів |

Використовується в:  
  - WarehouseController.getCompanies()  
  - Відображення списку фірм в інтерфейсі  
### 14. CreateCompanyDTO
Призначення: Створення нової фірми-постачальника.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| name | string | Так | 3-200 символів, унікальна назва |
| contactPerson | string | Так | 3-100 символів |
| phone | string | Так | Формат: +380XXXXXXXXX |
| email | string | Так | Валідний email, унікальний |
| address | string | Так | Макс. 500 символів |
| notes | string | Ні | Макс. 1000 символів |

Використовується в:  
  - WarehouseController.createCompany()  
### 15. UpdateCompanyDTO
Призначення: Оновлення даних існуючої фірми.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| name | string | Так | 3-200 символів, унікальна назва |
| contactPerson | string | Так | 3-100 символів |
| phone | string | Так | Формат: +380XXXXXXXXX |
| email | string | Так | Валідний email, унікальний |
| address | string | Так | Макс. 500 символів |
| notes | string | Ні | Макс. 1000 символів |

Використовується в:  
  - WarehouseController.updateCompany()  
### 16. UpdateStatusDTO
Призначення: Зміна статусу замовлення адміністратором.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| newStatus | OrderStatus | Так | Processing/Shipped/Delivered/Cancelled |
| comment | string | Ні | Макс. 500 символів (ТТН) |
| reason | string | Так (якщо Cancelled) | Причина скасування |

Використовується в:  
  - AdminController.updateOrderStatus()  
### 17. CreateSessionDTO
Призначення: Створення платіжної сесії Stripe.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| orderId | int | Так | Існуюче замовлення |
| amount | decimal | Так | > 0 |
| currency | string | Так | UAH |

Використовується в:  
  - PaymentController.createSession()  
### 18. CalculateCostDTO
Призначення: Розрахунок вартості доставки.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| cityRef | string | Так | Ref з Nova Poshta API |
| warehouseRef | string | Так | Ref з Nova Poshta API |
| weight | decimal | Так | > 0 (сумарна вага товарів) |

Використовується в:  
  - ShippingController.calculateCost()  
### 19. PromotionDTO
Призначення: Базовий DTO для передачі даних акції (відображення).  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| id | int | Так | ID акції |
| name | string | Так | Назва акції |
| description | string | Ні | Опис акції |
| type | string | Так | Тип знижки (PERCENTAGE/ FIXED\_AMOUNT/ SPECIAL\_PRICE) |
| value | decimal | Так | Значення знижки |
| targetType | string | Так | PRODUCT/CATEGORY/ CART/SHIPPING |
| targetId | int | Ні | ID товару/категорії (nullable) |
| audienceType | string | Так | ALL/STUDENTS/STAFF /ALUMNI/CUSTOM |
| startDate | Date | Ні | Дата початку (nullable) |
| endDate | Date | Ні | Дата закінчення (nullable) |
| promoCode | string | Ні | Промокод (nullable) |
| minOrderAmount | decimal | Ні | Мінімальна сума (nullable) |
| minQuantity | int | Ні | Мінімальна кількість (nullable) |
| priority | int | Так | Пріоритет |
| usageLimit | int | Ні | Ліміт використань (nullable) |
| currentUsage | int | Так | Поточна кількість використань |
| isActive | bool | Так | Чи активна |
| createdAt | int | Так | Дата створення |

Використовується в:  
  - PromotionController.getAll()  
  - PromotionController.getById()  
  - UserController.getMyPromotions()  
### 20. CreatePromotionDTO
Призначення: Створення нової акції/знижки менеджером.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| name | string | Так | 3-200 символів |
| description | string | Ні | Макс. 2000 символів |
| type | string | Так | PERCENTAGE/FIXED\_AMOUNT /SPECIAL\_PRICE |
| value | decimal | Так | > 0, для PERCENTAGE <= 100 |
| targetType | string | Так | PRODUCT/CATEGORY/CART/SHIPPING |
| targetId | int | Умовно | Обов'язковий якщо PRODUCT/CATEGORY |
| audienceType | string | Так | ALL/STUDENTS/STAFF/ALUMNI/CUSTOM |
| startDate | Date | Ні | >= поточна дата (nullable) |
| endDate | Date | Ні | > startDate (nullable) |
| promoCode | string | Ні | 3-50 символів, унікальний (nullable) |
| minOrderAmount | decimal | Ні | >= 0 (nullable) |
| minQuantity | int | Ні | >= 1 (nullable) |
| priority | int | Ні | 0-100 (default: 0) |
| usageLimit | int | Ні | >= 1 (nullable) |

Правила валідації:  
  - Якщо type = PERCENTAGE, value <= 100  
  - Якщо type = SPECIAL\_PRICE, value < ціни товару  
  - Якщо targetType = PRODUCT, targetId обов'язковий та існує в Products  
  - Якщо targetType = CATEGORY, targetId обов'язковий та існує в Categories  
  - Якщо promoCode вказано, перевірити унікальність  
  - endDate > startDate (якщо обидва вказані)  

Використовується в:  
  - PromotionController.create()  
### 21. UpdatePromotionDTO
Призначення: Оновлення існуючої акції.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| name | string | Так | 3-200 символів |
| description | string | Ні | Макс. 2000 символів |
| type | string | Так | PERCENTAGE/FIXED\_AMOUNT /SPECIAL\_PRICE |
| value | decimal | Так | > 0, для PERCENTAGE <= 100 |
| targetType | string | Так | PRODUCT/CATEGORY/CART/SHIPPING |
| targetId | int | Умовно | Обов'язковий якщо PRODUCT/CATEGORY |
| audienceType | string | Так | ALL/STUDENTS/STAFF/ALUMNI/CUSTOM |
| startDate | Date | Ні | >= поточна дата (nullable) |
| endDate | Date | Ні | > startDate (nullable) |
| promoCode | string | Ні | 3-50 символів, унікальний (nullable) |
| minOrderAmount | decimal | Ні | >= 0 (nullable) |
| minQuantity | int | Ні | >= 1 (nullable) |
| priority | int | Ні | 0-100 (default: 0) |
| usageLimit | int | Ні | >= 1 (nullable) |

Використовується в:  
  - PromotionController.update()  
### 22. ApplyPromoCodeDTO
Призначення: Застосування промокоду при оформленні замовлення.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| promoCode | string | Так | Промокод для перевірки |
| userId | int | Так | ID користувача |
| cartItems | List<CartItemDTO> | Так | Товари в кошику для валідації |

Використовується в:  
  - PromotionController.validatePromoCode()  
### 23. AssignPromotionDTO
Призначення: Призначення персональної знижки користувачу менеджером.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| userId | int | Так | Існуючий користувач |
| promotionId | int | Так | Існуюча акція |

Правила валідації:  
  - userId існує в Users  
  - promotionId існує в Promotions та isActive = true  
  - Комбінація (userId, promotionId) унікальна в UserPromotions  

Використовується в:  
  - PromotionController.assignToUser()  
### 24. StudentVerificationDTO
Призначення: Результат верифікації студента через University API.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| found | bool | Так | Чи знайдено студента в базі університету |
| studentId | string | Ні | ID студента в університетській системі |
| email | string | Так | Email студента |
| gpa | decimal | Ні | Середній бал (0.00-5.00) |
| scholarshipStatus | string | Ні | ACTIVE/INACTIVE |
| studentStatus | string | Ні | REGULAR/SCHOLARSHIP/ HIGH\_ACHIEVER |
| enrollmentYear | int | Ні | Рік вступу |
| faculty | string | Ні | Факультет |
| specialty | string | Ні | Спеціальність |

Використовується в:  
  - UniversityIntegration.verifyStudent()  
  - AuthController.register() (внутрішньо)  
### 25. ProductFilters (Query параметри)
Призначення: Фільтрація та пагінація товарів.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| page | int | Ні | Номер сторінки (default: 1) |
| limit | int | Ні | Кількість на сторінці (default: 20) |
| categoryId | int | Ні | Фільтр за категорією |
| minPrice | decimal | Ні | Мінімальна ціна |
| maxPrice | decimal | Ні | Максимальна ціна |
| search | string | Ні | Пошук за назвою |
| sort | string | Ні | Сортування (price/name/createdAt) |
| order | string | Ні | Порядок (asc/desc) |

Використовується в:  
  - ProductController.getAll()  
### 26. OrderFilters (Query параметри)
Призначення: Фільтрація замовлень користувача.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| page | int | Ні | Номер сторінки (default: 1) |
| limit | int | Ні | Кількість на сторінці (default: 10) |
| status | OrderStatus | Ні | Фільтр за статусом |
| startDate | Date | Ні | Початкова дата |
| endDate | Date | Ні | Кінцева дата |

Використовується в:  
  - OrderController.getHistory()  
### 27. AdminOrderFilters (Query параметри)
Призначення: Фільтрація всіх замовлень для адміністратора.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| page | int | Ні | Номер сторінки (default: 1) |
| limit | int | Ні | Кількість на сторінці (default: 20) |
| status | OrderStatus | Ні | Фільтр за статусом |
| userId | int | Ні | Фільтр за користувачем |
| startDate | Date | Ні | Початкова дата |
| endDate | Date | Ні | Кінцева дата |
| orderNumber | string | Ні | Пошук за номером |

Використовується в:  
  - AdminController.getAllOrders()  
### 28. UserFilters (Query параметри)
Призначення: Фільтрація користувачів для адміністратора.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| page | int | Ні | Номер сторінки (default: 1) |
| limit | int | Ні | Кількість на сторінці (default: 20) |
| role | Role | Ні | Фільтр за роллю |
| status | UserStatus | Ні | Фільтр за статусом |
| search | string | Ні | Пошук за ім'ям/email |

Використовується в:  
  - AdminController.getAllUsers()  
### 29. DocFilters (Query параметри)
Призначення: Фільтрація складських документів.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| page | int | Ні | Номер сторінки (default: 1) |
| limit | int | Ні | Кількість на сторінці (default: 20) |
| productId | int | Ні | Фільтр за товаром |
| startDate | Date | Ні | Початкова дата |
| endDate | Date | Ні | Кінцева дата |
| createdBy | int | Ні | Хто створив (для IncomingDocuments) |
| reason | OutgoingReason | Ні | Причина (для OutgoingDocuments) |

Використовується в:  
  - WarehouseController.getIncomingHistory()  
  - WarehouseController.getOutgoingHistory()  
### 30. StockFilters (Query параметри)
Призначення: Фільтрація залишків товарів.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| page | int | Ні | Номер сторінки (default: 1) |
| limit | int | Ні | Кількість на сторінці (default: 50) |
| categoryId | int | Ні | Фільтр за категорією |
| lowStock | bool | Ні | Тільки товари з низьким запасом (< 10) |
| outOfStock | bool | Ні | Тільки товари без залишків (= 0) |

Використовується в:  
  - WarehouseController.getCurrentStock()  
### 31. PromotionFilters (Query параметри)
Призначення: Фільтрація акцій та знижок.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| page | int | Ні | Номер сторінки (default: 1) |
| limit | int | Ні | Кількість на сторінці (default: 20) |
| isActive | bool | Ні | Фільтр за статусом (true/false/null) |
| type | PromotionType | Ні | Фільтр за типом знижки |
| targetType | PromotionTarget | Ні | Фільтр за областю застосування |
| audienceType | PromotionAudience | Ні | Фільтр за цільовою аудиторією |
| startDate | Date | Ні | Фільтр: акції що почалися після цієї дати |
| endDate | Date | Ні | Фільтр: акції що закінчуються до цієї дати |
| search | string | Ні | Пошук за назвою акції або промокодом |
| sort | string | Ні | Сортування (name/createdAt/currentUsage/priority) |
| order | string | Ні | Порядок (asc/desc) |

Використовується в:  
  - PromotionController.getAll()  
  - AdminController.getAllPromotions()  
### 32. AnalyticsFilters (Query параметри)
Призначення: Базові параметри фільтрації для всіх типів аналітики.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| startDate | DateTime | Так | Початок періоду аналізу |
| endDate | DateTime | Так | Кінець періоду аналізу |
| productId | int | Ні | Фільтр за конкретним товаром (nullable) |
| categoryId | int | Ні | Фільтр за категорією (nullable) |
| groupBy | string | Ні | Групування: day/week/month (default: day) |

Правила валідації:  
  - startDate <= endDate  
  - Період не більше 2 років (endDate - startDate <= 730 days)  
  - groupBy має бути один з: "day", "week", "month"  

Використовується в:  
  - AnalyticsController.getSalesAnalytics()  
  - AnalyticsController.getCategoryAnalytics()  
### 33. DateRange
Призначення: Представлення часового діапазону для аналітики.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| startDate | DateTime | Так | Дата початку періоду |
| endDate | DateTime | Так | Дата закінчення періоду |

Використовується в:  
  - SalesAnalyticsDTO  
  - ProductAnalyticsDTO  
  - CategoryAnalyticsDTO  
  - ConversionMetricsDTO  
  - FinancialReportDTO  
  - PromotionEffectivenessDTO  
  - UserBehaviorDTO  
### 34. SalesAnalyticsDTO
Призначення: Результат аналізу продажів за період з детальною розбивкою.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| period | DateRange | Так | Період аналізу |
| totalOrders | int | Так | Загальна кількість замовлень |
| totalRevenue | decimal | Так | Загальний дохід (грн) |
| averageOrderValue | decimal | Так | Середній чек |
| salesByDay | List<DailyMetric> | Так | Динаміка по днях/тижнях/місяцях |
| salesByPaymentMethod | Dictionary<PaymentMethod, decimal> | Так | Розподіл за способами оплати |
| salesByStatus | Dictionary<OrderStatus, int> | Так | Розподіл за статусами |
| topProducts | List<ProductMetric> | Так | Топ-10 товарів |
| changeVsPrevious | ChangeMetrics | Ні | Зміни відносно попереднього періоду |

Вкладені класи:  
  - DailyMetric: { date: DateTime, revenue: decimal, orders: int }  
  - ChangeMetrics: { ordersChange: decimal, revenueChange: decimal, avgOrderChange: decimal }  

Використовується в:  
  - AnalyticsController.getSalesAnalytics()  
  - AnalyticsService.getSalesAnalytics()  
### 35. ProductAnalyticsDTO
Призначення: Аналіз популярності товарів за різними метриками.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| period | DateRange | Так | Період аналізу |
| topByQuantity | List<ProductMetric> | Так | Топ товарів за кількістю продажів |
| topByRevenue | List<ProductMetric> | Так | Топ товарів за доходом |
| topByProfit | List<ProductMetric> | Так | Топ товарів за прибутком |
| lowStock | List<ProductMetric> | Так | Товари з низькими залишками |

Вкладені класи:  
  - ProductStock: { productId: int, productName: string, currentStock: int, minStock: int }  

Використовується в:  
  - AnalyticsController.getProductPopularity()  
### 36. CategoryAnalyticsDTO
Призначення: Аналітика продажів по категоріях товарів.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| period | DateRange | Так | Період аналізу |
| revenueByCategory | List<CategoryMetric> | Так | Дохід по категоріях |
| quantityByCategory | List<CategoryMetric> | Так | Кількість по категоріях |
| categoryShare | List<CategoryMetric> | Так | Частка категорій в загальному доході |

Вкладені класи:  
  - CategoryShare: { categoryId: int, categoryName: string, sharePercent: decimal }  

Використовується в:  
  - AnalyticsController.getCategoryAnalytics()  
### 37. ConversionMetricsDTO
Призначення: Метрики конверсії та ефективності магазину.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| period | DateRange | Так | Період аналізу |
| visitorToUserRate | decimal | Так | Конверсія відвідувачів → користувачів (%) |
| userToBuyerRate | decimal | Так | Конверсія користувачів → покупців (%) |
| cancelledOrdersRate | decimal | Так | Відсоток скасованих замовлень (%) |
| averageOrderValue | decimal | Так | Середній чек (грн) |
| repeatPurchaseRate | decimal | Так | Repeat Purchase Rate (%) |
| averageOrderItems | decimal | Так | Середня кількість товарів в замовленні |
| cartAbandonmentRate | decimal | Так | Cart Abandonment Rate (%) |

Використовується в:  
  - AnalyticsController.getConversionMetrics()  
### 38. FinancialReportDTO
Призначення: Фінансовий звіт з розрахунком прибутку та ROI.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| period | DateRange | Taк | Період аналізу |
| totalRevenue | decimal | Так | Загальний дохід (грн) |
| shippingRevenue | decimal | Так | Дохід від доставки (грн) |
| purchaseCosts | decimal | Так | Витрати на закупівлю (грн) |
| discountAmount | decimal | Так | Сума наданих знижок (грн) |
| netProfit | decimal | Так | Чистий прибуток (грн) |
| roiByProduct | List <ProductROI> | Так | ROI по товарах |
| roiByCategory | List <CategoryROI> | Так | ROI по категоріях |
| profitMargin | decimal | Так | Рентабельність (%) |

Вкладені класи:  
  - ProductROI: { productId: int, productName: string, revenue: decimal, cost: decimal, profit: decimal, roi: decimal }  
  - CategoryROI: { categoryId: int, categoryName: string, revenue: decimal, cost: decimal, profit: decimal, roi: decimal }  

Використовується в:  
  - AnalyticsController.getFinancialReport()  
### 39. PromotionEffectivenessDTO
Призначення: Детальна аналітика ефективності конкретної акції.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| promotionId | int | Так | ID акції |
| promotionName | string | Так | Назва акції |
| period | DateRange | Так | Період аналізу |
| totalUsages | int | Так | Кількість використань |
| totalDiscount | decimal | Так | Загальна сума знижок |
| averageOrderValueWithPromo | decimal | Так | Середній чек зі знижкою |
| averageOrderValueWithoutPromo | decimal | Так | Середній чек без знижки |
| conversionRateWithPromo | decimal | Так | Конверсія зі знижкою |
| conversionRateWithoutPromo | decimal | Так | Конверсія без знижки |
| roi | decimal | Так | ROI акції (%) |

Використовується в:  
  - AnalyticsController.getPromotionEffectiveness()  
### 40. UserBehaviorDTO
Призначення: Аналіз поведінки користувачів магазину.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| period | DateRange | Так | Періоданалізу |
| topCustomers | List<CustomerMetric> | Так | Топ-клієнти |
| averageTimeToFirstPurchase | decimal | Так | Середнійчас допершоїпокупки(днів) |
| cohortAnalysis | List<CohortData> | Так | Когортнийаналіз |
| categoryHeatmap | Dictionary<string,Dictionary<int, int>> | Так | Heat mapкатегорійпо днях |

Вкладені класи:  
  - CustomerMetric: { userId: int, orderCount: int, totalSpent: decimal }  
  - CohortData: { month: string, newUsers: int, retentionRate: decimal }  

Використовується в:  
  - AnalyticsController.getUserBehaviorAnalytics()  
### 41. ProductMetric
Призначення: Метрики продуктивності окремого товару.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| productId | int | Так | ID товару |
| productName | string | Так | Назва товару |
| quantity | int | Так | Кількість проданих одиниць |
| revenue | decimal | Так | Дохід від товару (грн) |
| profit | decimal | Ні | Прибуток від товару (грн, nullable) |

Використовується в:  
  - SalesAnalyticsDTO (вкладений в topProducts)  
  - ProductAnalyticsDTO (вкладений в topByQuantity, topByRevenue, topByProfit)  
### 42. CategoryMetric
Призначення: Метрики продуктивності категорії товарів.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| categoryId | int | Так | ID категорії |
| categoryName | string | Так | Назва категорії |
| quantity | int | Так | Кількість проданих одиниць |
| revenue | decimal | Так | Дохід від категорії (грн) |
| averageOrderValue | decimal | Так | Середній чек по категорії (грн) |

Використовується в:  
  - CategoryAnalyticsDTO (вкладений в revenueByCategory, quantityByCategory)  
### 43. ExportReportDTO
Призначення: Параметри для експорту аналітичного звіту.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| format | string | Так | Формат експорту:PDF/CSV/Excel |
| reportType | string | Так | Тип звіту: sales/products/categories/financial/conversion/behavior |
| period | DateRange | Так | Період для звіту |
| filters | Dictionary<string, object> | Ні | Додаткові фільтри (nullable) |

Правила валідації:  
  - format має бути один з: "PDF", "CSV", "Excel"  
  - reportType має бути один з: "sales", "products", "categories", "financial", "conversion", "behavior", "promotion"  

Використовується в:  
  - AnalyticsController.exportReport()  
### 44. PopularityFilters (Query параметри)**  
Призначення: Фільтри для аналізу популярності товарів.  
Поля:  

| Поле | Тип | Обов’язкове | Опис |
|------|-----|-------------|------|
| period | DateRange | Так | Період аналізу |
| metric | string | Так | Метрика: quantity/revenue/profit |
| categoryId | int | Ні | Фільтр за категорією (nullable) |
| limit | int | Ні | Кількість товарів (default: 10, max: 100) |

Правила валідації:  
  - metric має бути один з: "quantity", "revenue", "profit"  
  - limit має бути в діапазоні 1-100  

Використовується в:  
  - AnalyticsController.getProductPopularity()
## Enumerations
**1. Role (Роль користувача)**  
```
enum Role
{
    GUEST,      // Неавторизований відвідувач
    CUSTOMER,   // Зареєстрований клієнт
    MANAGER,    // Менеджер каталогу та складу
    ADMIN,      // Адміністратор замовлень та користувачів
    SUPERADMIN  // Технічний адміністратор системи
}
```
Використовується в:  
  - User.role  
  - JWT токенах для авторизації  

**2. UserStatus (Статус користувача)**  
```
enum UserStatus
{
    ACTIVE,   // Активний обліковий запис
    BLOCKED,  // Заблокований адміністратором
    DELETED   // Видалений (м'яке видалення)
}
```
Використовується в:  
  - User.status  

**3. OrderStatus (Статус замовлення)**  
```
enum OrderStatus
{
    PENDING_PAYMENT,  // Очікує оплати (початковий статус)
    PROCESSING,       // Оплачено, обробляється
    SHIPPED,          // Відправлено
    DELIVERED,        // Доставлено
    CANCELLED         // Скасовано
}
```
Дозволені переходи:  
  - PENDING\_PAYMENT → PROCESSING (після успішної оплати)  
  - PENDING\_PAYMENT → CANCELLED (скасування до оплати)  
  - PROCESSING → SHIPPED (відправка)  
  - PROCESSING → CANCELLED (скасування після оплати)  
  - SHIPPED → DELIVERED (доставка)  
  - SHIPPED → CANCELLED (скасування при доставці - рідко)  

Використовується в:  
  - Order.status  
  - OrderHistory.oldStatus, OrderHistory.newStatus  

**4. PaymentMethod (Спосіб оплати)**  
```
enum PaymentMethod 
{
    CARD\_ONLINE,       // Онлайн-оплата карткою через Stripe
    CASH\_ON\_DELIVERY  // Оплата при отриманні на відділенні
}
```
Використовується в:  
  - Order.paymentMethod  
  - Payment.method  

**5. PaymentStatus (Статус платежу)**  
```
enum PaymentStatus 
{
    PENDING,  // Очікує оплати
    PAID,     // Оплачено
    FAILED,   // Помилка оплати
    REFUNDED  // Повернено кошти
}
```
Використовується в:  
  - Payment.status  

**6. OutgoingReason (Причина списання)**
```
enum OutgoingReason 
{
    ORDER,     // Списання через замовлення (автоматично)
    DAMAGED,   // Пошкоджений товар
    LOST,      // Втрата товару
    RETURN,    // Повернення постачальнику
    INVENTORY  // Інвентаризація (коригування)
}
```
Використовується в:  
  - OutgoingDocument.reason  

**7. PromotionType (Тип знижки)**  
```
enum PromotionType 
{
    PERCENTAGE,     // Відсоткова знижка (наприклад, 15%)
    FIXED\_AMOUNT,  // Фіксована сума (наприклад, -50 грн)
    SPECIAL\_PRICE  // Спеціальна ціна (наприклад, 299 грн замість 450 грн)
}
```
Використовується в:  
  - Promotion.type  
  - CreatePromotionDTO.type  

**8. PromotionTarget (Область застосування знижки)**  
```
enum PromotionTarget 
{
    PRODUCT,   // Знижка на конкретний товар
    CATEGORY,  // Знижка на категорію товарів
    CART,      // Знижка на весь кошик (при досягненні мін. суми)
    SHIPPING   // Знижка на доставку
}
```
Використовується в:  
  - Promotion.targetType  
  - CreatePromotionDTO.targetType  

**9. PromotionAudience (Цільова аудиторія)**  
```
enum PromotionAudience 
{
    ALL,       // Всі користувачі
    STUDENTS,  // Студенти (studentStatus != NONE)
    STAFF,     // Працівники університету
    ALUMNI,    // Випускники
    CUSTOM     // Персонально призначені користувачі
}
```
Використовується в:  
  - Promotion.audienceType  
  - CreatePromotionDTO.audienceType  

**10. StudentStatus (Статус студента)**  
```
enum StudentStatus 
{
    NONE,           // Не студент або не верифіковано
    REGULAR,        // Звичайний студент
    SCHOLARSHIP,    // Стипендіат
    HIGH\_ACHIEVER  // Відмінник (GPA >= 4.5 та стипендіат)
}
```
Використовується в:  
  - User.studentStatus  
  - StudentVerificationDTO.studentStatus  
  - JWT токенах для швидкого доступу до статусу  

Логіка визначення studentStatus при верифікації через University API:  
  - GPA >= 4.5 AND scholarshipStatus = ACTIVE → HIGH\_ACHIEVER  
  - scholarshipStatus = ACTIVE → SCHOLARSHIP  
  - інакше → REGULAR
## Взаємозв'язки між структурами
**1. Життєвий цикл замовлення**  
```
User (Користувач)  
├─> studentStatus (REGULAR/SCHOLARSHIP/HIGH\_ACHIEVER)
├─> UserPromotion[] (Персональні знижки)
├─> Cart (Кошик)
├─> CartItem[] (Товари в кошику)
└─> Product (Посилання на товар)
```
Перед оформленням замовлення:  
  - Застосовується знижка - PromotionService.getApplicablePromotions(userId, cartItems):  
      1. Отримує User.studentStatus, User.gpa, User.studentExpiresAt  
      2. Перевіряє чи активний studentStatus (studentExpiresAt > now)  
      3. Збирає доступні знижки:  
          - Студентські знижки (за studentStatus, якщо активний)  
          - Персональні знижки (UserPromotions для userId)  
          - Товарні/Категорійні знижки (Promotions з targetType=PRODUCT/CATEGORY)  
          - Знижки на кошик (Promotions з targetType=CART)  
      4. Відсортовує за пріоритетом:  
          - Персональні (найвищий)  
          - Студентські (за studentStatus)  
          - Товарні/Категорійні  
          - Загальні акції  
      5. Для кожного товару вибирає найвигіднішу знижку  
      6. Розраховує попередні ціни: originalPrice, discountAmount, finalPrice  
  - Користувач може ввести промокод - PromotionService.validatePromoCode(code, userId, cartItems):  
      1. Шукає Promotion WHERE promoCode = code  
      2. Валідує: isActive, startDate/endDate, usageLimit, minOrderAmount, minQuantity  
      3. Якщо валідний → застосовує поверх автоматичних знижок (стакування)  
      4. Перераховує finalPrice з урахуванням промокоду  

Оформлення замовлення:  
  - CartItem[] → OrderItem[] (копіювання з інформацією про знижки)  
```
Order (Замовлення)
├─> OrderItem[] (Товари зі знижками)
│   ├─> originalPrice (базова ціна)
│   ├─> appliedPromotionId (ID найвигіднішої знижки)
│   ├─> discountAmount (сума знижки)
│   └─> finalPrice (ціна після знижки)
├─> Payment (Платіж)
├─> Shipping (Доставка)
├─> OrderHistory[] (Історія змін)
└─> OutgoingDocument[] (Автоматичні видаткові накладні зі знижками)
```
  - Важливо: При створенні замовлення автоматично:  
      1. PromotionService.calculateFinalPrices() розраховує ціни зі знижками  
      2. Створюються OrderItem з повною інформацією про знижки:  
          - originalPrice (базова ціна товару)  
          - appliedPromotionId (ID застосованої знижки або промокоду)  
          - discountAmount (сума знижки в грн)  
          - finalPrice (фінальна ціна після знижки)  
      3. Створюється Payment з підсумковою сумою (враховуючи знижки)  
      4. Створюється Shipping з адресою доставки  
      5. Створюються OutgoingDocument для кожного товару:  
          - Reason = "ORDER"  
          - Копіюються дані про знижки з OrderItem (originalPrice, appliedPromotionId, discountAmount, finalPrice)  
      6. Оновлюється Promotion.currentUsage++ (якщо використано промокод)  
      7. Оновлюється UserPromotion.usedCount++ (якщо персональна знижка)  
      8. Оновлюється Product.Stock (зменшення)  
      9. Очищується Cart користувача  
      10. Надсилається Email з деталізацією знижок  

**2. Складський облік**  
```
Product (Товар)
├─> IncomingDocument[] (Надходження)
│   └─> увеличує Stock
└─> OutgoingDocument[] (Списання)
└─> зменшує Stock
```
- Stock розраховується: Stock = SUM(IncomingDocument.quantity) - SUM(OutgoingDocument.quantity)  
- Важливо:  
    + При створенні товару Stock = 0  
    + Stock оновлюється через WarehouseService  
    + Всі зміни Stock логуються через документи  

**3. Ієрархія категорій**  
```
Category (Батьківська)
└─> Category[] (Дочірні)
    └─> Category[] (Вкладені)
        └─> Product[] (Товари)
```
Приклад:  
```
Одяг (parentId: null)
├─> Футболки (parentId: 1)
│   └─> Product "Футболка ХДУ"
└─> Худі (parentId: 1)
    └─> Product "Худі ХДУ"
```
**4. Система знижок**  
```
Promotion (Акція/Знижка)
├─> PromotionType (PERCENTAGE/FIXED\_AMOUNT/SPECIAL\_PRICE)
├─> PromotionTarget (PRODUCT/CATEGORY/CART/SHIPPING)
├─> PromotionAudience (ALL/STUDENTS/STAFF/ALUMNI/CUSTOM)
└─> Умови (startDate, endDate, promoCode, minOrderAmount, minQuantity, priority, usageLimit)
```
- Автоматичне призначення студентських знижок:  
    1. При реєстрації з @ksu.edu.ua або @student.ksu.edu.ua  
    2. AuthService → UniversityIntegration.verifyStudent(email)  
    3. University API повертає: studentStatus, GPA, scholarshipStatus  
    4. AuthService оновлює User:  
        - studentStatus (REGULAR/SCHOLARSHIP/HIGH\_ACHIEVER)  
        - gpa (decimal 0.00-5.00)  
        - studentVerifiedAt (now())  
        - studentExpiresAt (now() + 4 місяці)  
    5. PromotionService.assignStudentPromotions(userId, studentStatus):  
        - Шукає Promotion WHERE audienceType = 'STUDENTS' AND isActive = true  
        - Фільтрує за studentStatus  
        - Створює UserPromotion для кожної відповідної знижки  
    6. NotificationService надсилає Email з інформацією про знижки  
- Стакування знижок при оформленні замовлення - PromotionService.calculateFinalPrices(cartItems, userId, promoCode):  
  FOR EACH item IN cartItems:  
  1. originalPrice = item.Product.Price  
  2. Відфільтрувати promotions які застосовуються до цього item  
  3. Відсортувати за пріоритетом:  
      - Priority 1: Персональні (audienceType = CUSTOM)  
      - Priority 2: Студентські (audienceType = STUDENTS, за studentStatus)  
      - Priority 3: Товарні/Категорійні (targetType = PRODUCT/CATEGORY)  
      - Priority 4: Загальні (audienceType = ALL)  
      - Priority 5: Промокод (якщо введено)  
  4. Застосувати знижку з найвищим пріоритетом  
  5. Якщо введено промокод і він дає більшу знижку → використати промокод  
  6. finalPrice = originalPrice - discountAmount  
  7. appliedPromotionId = promotion.id  
  RETURN items з розрахованими цінами  
- Зв'язки:  
    + Promotion 1:N UserPromotion (персональні призначення)  
    + User 1:N UserPromotion (користувачі з персональними знижками)  
    + Promotion 1:N OrderItem (знижки в замовленнях)  
    + Promotion 1:N OutgoingDocument (знижки при списанні)  
    + Promotion N:1 Product (знижки на товар, якщо targetType=PRODUCT)  
    + Promotion N:1 Category (знижки на категорію, якщо targetType=CATEGORY)  

**5. Користувач та його дані**  
```
User
├─> studentStatus (NONE/REGULAR/SCHOLARSHIP/HIGH\_ACHIEVER)
├─> gpa (decimal 0.00-5.00)
├─> studentVerifiedAt, studentExpiresAt (термін дії статусу)
├─> Cart (1:1) - один кошик
├─> Order[] (1:N) - багато замовлень
├─> Shipping[] (1:N) - збережені адреси
├─> UserPromotion[] (1:N) - персональні знижки
├─> IncomingDocument[] (1:N) - створені накладні (якщо Manager)
└─> OutgoingDocument[] (1:N) - створені накладні (якщо Manager/Admin)
```
**6. Аналітика та звітність**  
```
AnalyticsService (Сервіс аналітики)
├─> OrderRepository (читає Orders, OrderItems для аналізу продажів)
├─> ProductRepository (читає Products для аналізу популярності)
├─> CategoryRepository (читає Categories для аналізу категорій)
├─> PromotionRepository (читає Promotions для аналізу ефективності акцій)
├─> IncomingRepository (читає IncomingDocuments для розрахунку ROI)
├─> UserRepository (читає Users для аналізу поведінки)
└─> CacheService (кешує розраховані метрики, TTL = 15 хвилин)
```
- Процес генерації аналітики:  
  1. AnalyticsController отримує запит від Менеджера/Адміністратора  
  2. Валідує параметри (період, фільтри)  
  3. Перевіряє права доступу (MANAGER або ADMIN)  
  4. Передає в AnalyticsService  
  5. AnalyticsService формує ключ кешу на основі параметрів  
  6. Перевіряє наявність даних у кеші  
  7. Якщо кеш актуальний → повертає з кешу  
  8. Якщо кеш відсутній або застарілий:  
      1) Запитує дані з репозиторіїв (Orders, Products, etc.)  
      2) Розраховує метрики паралельно (Task.WhenAll):  
          - Aggregate metrics (totalOrders, totalRevenue, avgOrderValue)  
          - Time series (salesByDay/Week/Month)  
          - Distributions (salesByPaymentMethod, salesByStatus)  
          - Top lists (topProducts, topCategories)  
          - Comparisons (changeVsPreviousPeriod)  
      3) Формує відповідний DTO (SalesAnalyticsDTO, ProductAnalyticsDTO, etc.)  
      4) Зберігає результат у кеш (TTL = 15 хвилин)  
      5) Логує подію через Serilog  
  9. Повертає DTO в AnalyticsController  
  10. Controller повертає HTTP 200 OK з даними  
- Експорт звітів:  
    1. Користувач натискає "Експортувати" та обирає формат (PDF/CSV/Excel)  
    2. AnalyticsController отримує ExportReportDTO  
    3. AnalyticsService отримує дані аналітики (з кешу або розраховує)  
    4. ReportService генерує файл у обраному форматі:  
        - PDF: iText7, включає графіки як зображення  
        - Excel: EPPlus, декілька листів з форматуванням  
        - CSV: CsvHelper, тільки табличні дані  
    5. Повертає файл для завантаження (Content-Type: application/pdf|xlsx|csv)  
- Кешування метрик:  
    + Ключ кешу формується з параметрів: "analytics:{type}:{period}:{filters}"  
    + TTL = 15 хвилин для операційних звітів  
    + TTL = 30 хвилин для фінансових звітів (більш стабільні дані)  
    + TTL = 1 година для метрик конверсії (змінюються повільно)  
    + Інвалідація кешу при створенні нових замовлень (опціонально)  
- Зв'язки між DTOs:  
```
SalesAnalyticsDTO
├─> DateRange (період аналізу)
├─> List<ProductMetric> (топ товарів)
└─> ChangeMetrics (зміни відносно попереднього періоду)
```
```
ProductAnalyticsDTO
├─> DateRange (період аналізу)
└─> List<ProductMetric> (топи за різними метриками)
```
```
CategoryAnalyticsDTO
├─> DateRange (період аналізу)
├─> List<CategoryMetric> (метрики категорій)
└─> List<CategoryShare> (частки категорій)
```
```
FinancialReportDTO
├─> DateRange (період аналізу)
├─> List<ProductROI> (ROI по товарах)
└─> List<CategoryROI> (ROI по категоріях)
```
```
UserBehaviorDTO
├─> DateRange (період аналізу)
├─> List<CustomerMetric> (топ-клієнти)
└─> List<CohortData> (когортний аналіз)
```
## Правила валідації
**1. Валідація на рівні Entity**  
User:  
  - email - унікальний в системі  
  - passwordHash - bcrypt з salt rounds = 12  
  - role - default: CUSTOMER  

Product:  
  - price > 0  
  - weight > 0  
  - Унікальність (name, categoryId) - попередження, не помилка  

Order:  
  - orderNumber - генерується автоматично (format: "ORD-YYYYMMDD-XXXXX")  
  - totalAmount >= 100 (мінімальна сума замовлення)  

CartItem:  
  - Унікальний (cartId, productId) - один товар раз у кошику  
  - quantity <= Product.Stock  

**2. Валідація на рівні DTO**  
- RegisterDTO:  
```js
{
    firstName:
    {
        required: true,
        minLength: 2,
        maxLength: 50,
        pattern: /^[А-ЯІЇЄҐа-яіїєґA-Za-z\s'-]+$/
    },
    email:
    {
        required: true,
        email: true,
        unique: true // перевірка в БД
    },
    password:
    {
        required: true,
        minLength: 8,
        pattern: /^(?=.\*[A-Za-z])(?=.\*\d).+$/ // букви + цифри
    },
    phone:
    {
        optional: true,
        pattern: /^\+380\d{9}$/
    }
}
```
- ProductDTO:  
```js
{
    name:
    {
        required: true,
        minLength: 3,
        maxLength: 200
    },
    price:
    {
        required: true,
        min: 0.01,
        decimal: 2
    },
    weight:
    {
        required: true,
        min: 0.001,
        decimal: 3
    },
    images:
    {
        required: true, // для створення
        fileTypes: ['image/jpeg', 'image/png', 'image/webp'],
        maxSize: 5242880, // 5MB
        maxFiles: 10
    }
}
```
- IncomingDTO:  
```js
{
    quantity:
    {
        required: true,
        min: 1,
        integer: true
    },
    purchasePrice:
    {
        required: true,
        min: 0.01,
        decimal: 2
    },
    documentDate:
    {
        required: true,
        maxDate: 'today'
    }
}
```
- CreatePromotionDTO:
```js
{
    name:
    {
        required: true,
        minLength: 3,
        maxLength: 200
    },
    type:
    {
        required: true,
        enum: ['PERCENTAGE', 'FIXED\_AMOUNT', 'SPECIAL\_PRICE']
    },
    value:
    {
        required: true,
        min: 0.01,
        decimal: 2,
        custom: (value, dto) => 
        {
            if (dto.type === 'PERCENTAGE' && value > 100)
            {
                return 'Відсоткова знижка не може перевищувати 100%'
            }
        }
    },
    targetType:
    {
        required: true,
        enum: ['PRODUCT', 'CATEGORY', 'CART', 'SHIPPING']
    },
    targetId:
    {
        requiredIf: (dto) => ['PRODUCT', 'CATEGORY'].includes(dto.targetType),
        custom: (id, dto) => 
        {
            if (dto.targetType === 'PRODUCT')
            {
                // Перевірка існування в Products
            }
            if (dto.targetType === 'CATEGORY')
            {
                // Перевірка існування в Categories
            }
        }
    },
    audienceType:
    {
        required: true,
        enum: ['ALL', 'STUDENTS', 'STAFF', 'ALUMNI', 'CUSTOM']
    },
    startDate:
    {
        optional: true,
        minDate: 'today'
    },
    endDate:
    {
        optional: true,
        custom: (endDate, dto) => 
        {
            if (dto.startDate && endDate <= dto.startDate)
            {
                return 'endDate має бути пізніше startDate'
            }
        }
    },
    promoCode:
    {
        optional: true,
        minLength: 3,
        maxLength: 50,
        unique: true, // перевірка в БД
        pattern: /^[A-Z0-9]+$/ // тільки великі літери та цифри
    },
    minOrderAmount:
    {
        optional: true,
        min: 0,
        decimal: 2
    },
    minQuantity:
    {
        optional: true,
        min: 1,
        integer: true
    },
    priority:
    {
        optional: true,
        min: 0,
        max: 100,
        integer: true,
        default: 0
    },
    usageLimit:
    {
        optional: true,
        min: 1,
        integer: true
    }
}
```
- ApplyPromoCodeDTO:  
```js
{
    promoCode: 
    {
        required: true,
        minLength: 3,
        maxLength: 50,
        pattern: /^[A-Z0-9]+$/
    },
    userId: 
    {
        required: true,
        exists: 'Users' // перевірка існування
    },
    cartItems: 
    {
        required: true,
        minLength: 1,
        array: true
    }
}
```
- AnalyticsFilters:  
```js
{
    startDate: 
    {
        required: true,
        type: DateTime
    },
    endDate: 
    {
        required: true,
        type: DateTime,
        custom: (endDate, dto) => 
        {
            if (endDate < dto.startDate) 
            {
                return 'endDate має бути пізніше startDate'
            }
            const daysDiff = (endDate - dto.startDate) / (1000 \* 60 \* 60 \* 24);
            if (daysDiff > 730) 
            {
                return 'Період не може перевищувати 2 роки'
            }
        }
    },
    groupBy: 
    {
        optional: true,
        enum: ['day', 'week', 'month'],
        default: 'day'
    }
}
```
- ExportReportDTO:  
```js
{
    format:
    {
        required: true,
        enum: ['PDF', 'CSV', 'Excel']
    },
    reportType:
    {
        required: true,
        enum: ['sales', 'products', 'categories', 'financial', 'conversion', 'behavior', 'promotion']
    },
    period:
    {
        required: true,
        type: DateRange,
        validate: true // валідація вкладеного об'єкта
    }
}
```
- PopularityFilters:  
```js
{
    metric:
    {
        required: true,
        enum: ['quantity', 'revenue', 'profit']
    },
    limit:
    {
        optional: true,
        min: 1,
        max: 100,
        integer: true,
        default: 10
    },
    categoryId:
    {
        optional: true,
        exists: 'Categories' // перевірка існування категорії
    }
}
```
**3. Бізнес-правила валідації**  
- Створення замовлення:  
    1. Перевірити доступність товарів: CartItem.quantity <= Product.Stock  
    2. Перевірити мінімальну суму: Order.totalAmount >= 100 грн  
    3. Перевірити валідність адреси через Nova Poshta API  
    4. Для онлайн-оплати: створити Stripe сесію до збереження Order  
- Зміна статусу замовлення:  
    1. Перевірити дозволені переходи статусів  
    2. При скасуванні: повернути товари на склад (створити OutgoingDocument з від'ємною кількістю)  
    3. Логувати зміну в OrderHistory  
- Робота зі знижками:  
    1. При створенні акції:  
        - Перевірити валідність type та value  
        - Якщо type=PERCENTAGE → value <= 100  
        - Якщо type=SPECIAL\_PRICE → value < originalPrice товару  
        - Якщо targetType=PRODUCT/CATEGORY → targetId обов'язковий та існує  
        - Якщо promoCode вказано → перевірити унікальність  
        - endDate > startDate (якщо обидва вказані)  
    2. При застосуванні промокоду:  
        - Перевірити Promotion.isActive = true  
        - Перевірити startDate <= now <= endDate  
        - Перевірити currentUsage < usageLimit  
        - Перевірити minOrderAmount <= orderTotal  
        - Перевірити minQuantity <= totalQuantity  
        - Якщо всі перевірки пройдені → застосувати знижку  
    3. При оформленні замовлення зі знижками:  
        - Розрахувати finalPrice для кожного товару через PromotionService  
        - Перевірити що finalPrice >= 0 (мін. ціна = 0)  
        - Зберегти originalPrice, appliedPromotionId, discountAmount, finalPrice в OrderItem  
        - Скопіювати дані про знижки в OutgoingDocument (Reason=ORDER)  
        - Оновити Promotion.currentUsage++ (якщо промокод)  
        - Оновити UserPromotion.usedCount++ (якщо персональна)  
    4. При верифікації студента:  
        - Перевірити email.endsWith('@ksu.edu.ua') OR email.endsWith('@student.ksu.edu.ua')  
        - Викликати UniversityIntegration.verifyStudent(email)  
        - Якщо знайдено → оновити User (studentStatus, gpa, studentVerifiedAt, studentExpiresAt)  
        - Призначити студентські знижки через PromotionService  
        - Якщо API недоступний → створити задачу для повторної перевірки  
        - Надіслати Email з інформацією про знижки  
    5. Деактивація знижок при закінченні терміну:  
        - Автоматична задача (cron) перевіряє studentExpiresAt < now()  
        - Для користувачів з неактивним статусом → студентські знижки не застосовуються  
        - Email-нагадування за тиждень до закінчення терміну  
- Складський облік:  
    1. При прибутковій накладній: companyId має відповідати активній фірмі (isActive=true)  
    2. При видатковій накладній:  
        - quantity <= Product.Stock  
        - якщо reason=RETURN, то companyId обов'язковий та відповідає активній фірмі  
        - для інших причин companyId=NULL  
    3. Заборонити від'ємний Stock (крім коригування інвентаризації)  
    4. Попереджати про низький запас (Stock < 10)  
- Керування фірмами:  
    1. При створенні фірми: name та email унікальні в системі  
    2. При деактивації фірми: перевірити чи не використовується в активних накладних  
    3. Фізичне видалення фірми можливе тільки якщо немає жодних пов'язаних накладних  
    4. При оновленні фірми: якщо змінюється email, перевірити унікальність
## Висновки
Даний документ описує всі структури даних, необхідні для функціонування інтернет-магазину сувенірної продукції ХДУ. Структури розділені на два основні типи:
  - Entities (Domain Models):  
      + Відображають реальні об'єкти предметної області  
      + Містять бізнес-логіку в методах  
      + Мають відповідні таблиці в БД MySQL  
      + Використовують Entity Framework Core для ORM  
  - DTOs (Data Transfer Objects):  
      + Використовуються для передачі даних між шарами  
      + Містять правила валідації  
      + Не містять бізнес-логіки  
      + Забезпечують безпеку (не передають чутливі дані як passwordHash)  
      + Спеціалізовані DTOs для аналітики забезпечують структуровану передачу агрегованих даних та метрик  
  - Ключові принципи проектування:  
      + Розділення відповідальностей: Entity для бізнес-логіки, DTO для передачі даних  
      + Складський облік через документи: Stock розраховується автоматично  
      + Аудит змін: OrderHistory, логування через Serilog  
      + Валідація на всіх рівнях: Frontend → Backend → Database  
      + Безпека: Хешування паролів, JWT токени, RBAC  
      + Аналітичні DTOs: SalesAnalyticsDTO, ProductAnalyticsDTO, CategoryAnalyticsDTO, ConversionMetricsDTO, FinancialReportDTO для передачі розрахованих метрик