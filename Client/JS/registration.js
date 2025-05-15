document
  .querySelector(".form")
  .addEventListener("submit", async function (event) {
    event.preventDefault(); // מונע את רענון הדף

    // קבלת הנתונים מהטופס
    const fullName = document.querySelector("#full-name").value;
    const email = document.querySelector("#email").value;
    const phone = document.querySelector("#phone").value;
    const password = document.querySelector("#password").value;
    const confirmPassword = document.querySelector("#confirm-password").value;

    if (password !== confirmPassword) {
      alert("הסיסמאות אינן תואמות!");
      return;
    }

    // שליחת הנתונים ל-API
    try {
      const response = await fetch(
        "https://your-api-url.com/api/SimulationController/register",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            fullName: fullName,
            email: email,
            phone: phone,
            password: password,
          }),
        }
      );

      if (response.ok) {
        const data = await response.json();
        alert("נרשמת בהצלחה!");
        console.log(data); // הצגת התגובה מהשרת
      } else {
        alert("שגיאה בהרשמה. בדוק את הפרטים ונסה שוב.");
      }
    } catch (error) {
      console.error("שגיאה:", error);
      alert("אירעה שגיאה. נסה שוב מאוחר יותר.");
    }
  });

  