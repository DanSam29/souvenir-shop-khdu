/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{js,jsx,ts,tsx}', './public/index.html'],
  // Темна тема вмикається класом .dark на кореневому елементі (опціонально)
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Херсонський блакитний — основний брендовий колір
        brand: {
          DEFAULT: '#165DFF',
          dark: '#0E42D2',
          light: '#4C86FF',
        },
        // Золото — акцентний колір ХДУ
        gold: {
          DEFAULT: '#FFC107',
          dark: '#FFB300',
        },
        // Темно-сірий — основний текстовий колір
        ink: {
          DEFAULT: '#1F2937',
          light: '#6B7280',
        },
      },
    },
  },
  plugins: [],
};
