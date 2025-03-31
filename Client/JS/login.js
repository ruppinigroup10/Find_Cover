document
  .querySelector(".form")
  .addEventListener("submit", async function (event) {
    event.preventDefault(); // מונע את רענון הדף

    // קבלת הנתונים מהטופס
    const email = document.querySelector("#email").value;
    const password = document.querySelector("#password").value;

    // שליחת הנתונים ל-API
    try {
      const response = await fetch(
        "https://your-api-url.com/api/SimulationController/login",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            email: email,
            password: password,
          }),
        }
      );

      if (response.ok) {
        const data = await response.json();
        alert("התחברת בהצלחה!");
        console.log(data); // הצגת התגובה מהשרת
      } else {
        alert("שגיאה בהתחברות. בדוק את הפרטים ונסה שוב.");
      }
    } catch (error) {
      console.error("שגיאה:", error);
      alert("אירעה שגיאה. נסה שוב מאוחר יותר.");
    }
  });
