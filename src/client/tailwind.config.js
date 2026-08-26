/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: '#0b5fff',
          dark: '#0947c9'
        }
      }
    }
  },
  plugins: []
};
