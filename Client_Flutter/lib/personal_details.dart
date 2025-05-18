// Importing necessary packages for Flutter UI, HTTP requests, and JSON handling
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import 'local_storage_service.dart';

// Stateful widget for the Personal Details page
class PersonalDetailsPage extends StatefulWidget {
  final String username;
  final String email;
  final String phoneNumber;

  const PersonalDetailsPage({
    super.key,
    required this.username,
    required this.email,
    required this.phoneNumber,
  });

  @override
  _PersonalDetailsPageState createState() => _PersonalDetailsPageState();
}

class _PersonalDetailsPageState extends State<PersonalDetailsPage> {
  late final TextEditingController usernameController;
  late final TextEditingController emailController;
  late final TextEditingController phoneController;
  final TextEditingController passwordController = TextEditingController();

  bool _isLoading = true; // Add loading state
  bool _obscurePassword = false; // Default to showing the password

  @override
  void initState() {
    super.initState();
    usernameController = TextEditingController();
    emailController = TextEditingController();
    phoneController = TextEditingController();

    // Fetch user data from the server and update fields
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _fetchAndSetUserData();
    });
  }

  Future<void> _fetchAndSetUserData() async {
    try {
      final userData = await LocalStorageService.getUserData();
      setState(() {
        usernameController.text = userData['username'] ?? '';
        emailController.text = userData['email'] ?? '';
        phoneController.text = userData['phone_number'] ?? '';
        _isLoading = false; // Data loaded
      });
    } catch (e) {
      print('Error fetching user data: $e');
      setState(() {
        _isLoading = false; // Stop loading even if there's an error
      });
    }
  }

  @override
  void dispose() {
    usernameController.dispose();
    emailController.dispose();
    phoneController.dispose();
    passwordController.dispose();
    super.dispose();
  }

  Future<void> _updateUserData() async {
    try {
      final userData = await LocalStorageService.getUserData();
      final userId = userData['userId'] ?? userData['user_id'];
      final url = Uri.parse('https://localhost:7203/api/User/UpdateUser');
      final requestBody = {
        'userId': userId,
        'username': usernameController.text,
        'passwordHash': passwordController.text,
        'email': emailController.text,
        'phoneNumber': phoneController.text,
        'createdAt': DateTime.now().toIso8601String(),
        'isActive': true,
        'isProvider': true,
      };

      print('שולח לשרת את הנתונים הבאים:');
      print('userId: ${requestBody['userId']}');
      print('username: ${requestBody['username']}');
      print('passwordHash: ${requestBody['passwordHash']}');
      print('email: ${requestBody['email']}');
      print('phoneNumber: ${requestBody['phoneNumber']}');
      print('createdAt: ${requestBody['createdAt']}');
      print('isActive: ${requestBody['isActive']}');
      print('isProvider: ${requestBody['isProvider']}');

      final response = await http.put(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(requestBody),
      );

      if (response.statusCode == 200) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('העדכון בוצע בהצלחה!'),
            backgroundColor: Colors.green,
          ),
        );
      } else {
        print('שגיאה בעדכון הנתונים: ${response.body}');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('שגיאה בעדכון הנתונים: ${response.body}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } catch (e) {
      print('שגיאה בחיבור לשרת: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('שגיאה בחיבור לשרת.'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Center(
        child: CircularProgressIndicator(), // Show loading indicator
      );
    }

    return Directionality(
      textDirection: TextDirection.rtl, // כיווניות מימין לשמאל
      child: Scaffold(
        body: Padding(
          padding: const EdgeInsets.all(16.0),
          child: SingleChildScrollView(
            child: Column(
              children: [
                const SizedBox(height: 20),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    IconButton(
                      icon: const Icon(Icons.arrow_back_ios),
                      onPressed: () {
                        Navigator.pop(context); // חזרה לעמוד הקודם
                      },
                    ),
                    Image.asset(
                      'assets/images/LOGO1.png', // לוגו
                      width: 100,
                      height: 100,
                    ),
                    const SizedBox(width: 48),
                  ],
                ),
                const SizedBox(height: 10),
                const Text(
                  'פרטים אישיים',
                  style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 20),
                _buildTextField(
                  'שם מלא',
                  usernameController.text,
                  usernameController,
                  false,
                ),
                const SizedBox(height: 10),
                _buildTextField(
                  'כתובת מייל',
                  emailController.text,
                  emailController,
                  false,
                ),
                const SizedBox(height: 10),
                _buildTextField(
                  'מספר טלפון',
                  phoneController.text,
                  phoneController,
                  false,
                ),
                const SizedBox(height: 10),
                _buildPasswordField('סיסמה *', passwordController),
                const SizedBox(height: 20),
                ElevatedButton(
                  onPressed: _updateUserData,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(
                      horizontal: 40,
                      vertical: 15,
                    ),
                  ),
                  child: const Text('שמירה', style: TextStyle(fontSize: 16)),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildTextField(
    String label,
    String hint,
    TextEditingController controller,
    bool obscureText,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        TextField(
          controller: controller,
          obscureText: obscureText,
          decoration: InputDecoration(
            hintText: '', // Removed default hint text
            filled: true,
            fillColor: const Color(0xFFB0C4DE),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide.none,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildPasswordField(String label, TextEditingController controller) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 5),
        TextField(
          controller: controller,
          obscureText: _obscurePassword,
          decoration: InputDecoration(
            hintText: '*****',
            filled: true,
            fillColor: const Color(0xFFB0C4DE),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide.none,
            ),
            suffixIcon: Checkbox(
              value: !_obscurePassword, // ברירת מחדל מסומן
              onChanged: (bool? value) {
                setState(() {
                  _obscurePassword = !(value ?? false);
                });
              },
            ),
          ),
        ),
      ],
    );
  }
}
