// Importing necessary packages for Flutter UI, HTTP requests, and JSON handling
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import 'local_storage_service.dart';
import 'base_page.dart';

// Stateful widget for the Personal Details page
class PersonalDetailsPage extends StatefulWidget {
  const PersonalDetailsPage({super.key});

  @override
  _PersonalDetailsPageState createState() => _PersonalDetailsPageState();
}

class _PersonalDetailsPageState extends State<PersonalDetailsPage> {
  late final TextEditingController usernameController;
  late final TextEditingController emailController;
  late final TextEditingController phoneController;
  final TextEditingController passwordController = TextEditingController();

  final _formKey = GlobalKey<FormState>(); // Add form key

  bool _isLoading = true; // Add loading state
  bool _obscurePassword = false; // Default to showing the password
  int? userId; // שמור את ה-userId מה-local storage

  @override
  void initState() {
    super.initState();
    usernameController = TextEditingController();
    emailController = TextEditingController();
    phoneController = TextEditingController();
    _loadUserIdAndFetchData();
  }

  Future<void> _loadUserIdAndFetchData() async {
    // שליפת userId מה-local storage
    final userData = await LocalStorageService.getUserData();
    final id = userData['userId'] ?? userData['user_id'];
    if (id != null && id.toString().isNotEmpty) {
      setState(() {
        userId = int.tryParse(id.toString());
      });
      await _fetchAndSetUserData();
    } else {
      setState(() {
        _isLoading = false;
      });
    }
  }

  Future<void> _fetchAndSetUserData() async {
    if (userId == null) return;
    print(
      'DEBUG: Fetching user data for userId = $userId',
    ); // DEBUG: print userId sent to server
    print(
      'DEBUG: GET request to: https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/getUser?user_id=[33m[1m[4m$userId[0m',
    ); // DEBUG: print full GET URL
    setState(() {
      _isLoading = true;
    });
    try {
      final url = Uri.parse(
        'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/getUser?user_id=$userId',
      );
      print('DEBUG: GET userId sent to server: $userId');
      final response = await http.get(url);
      print('DEBUG: response.statusCode = \'${response.statusCode}\'');
      print('DEBUG: response.body = ${response.body}');
      dynamic data;
      try {
        data = jsonDecode(response.body);
      } catch (e) {
        print('DEBUG: jsonDecode failed: $e');
        setState(() {
          usernameController.text = '';
          emailController.text = '';
          phoneController.text = '';
          passwordController.text = '';
          _isLoading = false;
        });
        return;
      }
      print('DEBUG: decoded data = $data');
      if (response.statusCode == 200 &&
          data.containsKey('user') &&
          data['user'] != null) {
        final user = data['user'];
        print('DEBUG: user = $user');
        // Fetch password from local storage
        final userData = await LocalStorageService.getUserData();
        final password =
            userData['PasswordHash'] ??
            userData['passwordHash'] ??
            userData['password_hash'] ??
            '';
        setState(() {
          usernameController.text = user['username']?.toString() ?? '';
          emailController.text = user['email']?.toString() ?? '';
          phoneController.text = user['phone_number']?.toString() ?? '';
          if (passwordController.text.isEmpty) {
            passwordController.text = password;
          }
          _isLoading = false;
        });
      } else {
        print('DEBUG: user key missing or null in response (data = $data)');
        setState(() {
          usernameController.text = '';
          emailController.text = '';
          phoneController.text = '';
          passwordController.text = '';
          _isLoading = false;
        });
      }
    } catch (e) {
      print('שגיאה בקבלת נתוני משתמש מהשרת: $e');
      setState(() {
        _isLoading = false;
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
      // שליפת userId בלבד מה-local storage
      final userData = await LocalStorageService.getUserData();
      final userId = userData['userId'] ?? userData['user_id'];
      print('DEBUG: Updating user data for userId = $userId');
      // שליפת הסיסמה הקודמת מה-local storage
      final previousPassword =
          userData['passwordHash'] ?? userData['password_hash'] ?? '';
      final passwordToSend =
          passwordController.text.isNotEmpty
              ? passwordController.text
              : previousPassword;
      final url = Uri.parse(
        'https://proj.ruppin.ac.il/igroup10/test2/tar1/api/User/UpdateUser',
      );
      final requestBody = {
        'userId': userId,
        'username': usernameController.text,
        'passwordHash': passwordToSend,
        'email': emailController.text,
        'phoneNumber': phoneController.text,
        'createdAt':
            DateTime.now()
                .toUtc()
                .add(const Duration(hours: 3))
                .toIso8601String(),
        'isActive': true,
        'isProvider': true,
      };
      print('שולח לשרת את הנתונים הבאים:');
      print('userId: [33m[1m[4m${requestBody['userId']}[0m');
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
        // שמור את הסיסמה שהוזנה בשדה
        final currentPassword = passwordController.text;
        // רענון מהשרת אחרי עדכון (הצגת נתונים עדכניים בלבד) לאחר חצי שניה
        await Future.delayed(const Duration(seconds: 5));
        await _fetchAndSetUserData();
        // החזר את הסיסמה שהוזנה לשדה
        setState(() {
          passwordController.text = currentPassword;
        });
        // שמירת הסיסמה החדשה (או הישנה) ב-local storage
        await LocalStorageService.saveUserData({
          'user_id': userId,
          'username': usernameController.text,
          'email': emailController.text,
          'phone_number': phoneController.text,
          'is_active': true,
          'is_provider': true,
          'PasswordHash': passwordToSend, // שמירת הסיסמה הנוכחית
        });
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
      return const Center(child: CircularProgressIndicator());
    }
    return Directionality(
      textDirection: TextDirection.rtl,
      child: BasePage(
        child: Container(
          width: double.infinity,
          height: double.infinity,
          color: const Color(0xFFF7F7F7), // צבע רקע זהה למסך הניווט
          child: Padding(
            padding: const EdgeInsets.all(16.0),
            child: SingleChildScrollView(
              child: Column(
                children: [
                  const SizedBox(height: 30),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      IconButton(
                        icon: const Icon(Icons.arrow_back_ios),
                        onPressed: () {
                          Navigator.pop(context);
                        },
                      ),
                      Image.asset(
                        'assets/images/LOGO1.png',
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
                  _buildTextField('שם מלא', usernameController, false),
                  const SizedBox(height: 10),
                  _buildTextField('כתובת מייל', emailController, false),
                  const SizedBox(height: 10),
                  _buildTextField('מספר טלפון', phoneController, false),
                  const SizedBox(height: 10),
                  _buildPasswordField('סיסמה *', passwordController),
                  const SizedBox(height: 20),
                  ElevatedButton(
                    onPressed: () {
                      if (_formKey.currentState!.validate()) {
                        _updateUserData();
                      }
                    },
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
      ),
    );
  }

  Widget _buildTextField(
    String label,
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
        TextFormField(
          controller: controller,
          obscureText: obscureText,
          decoration: InputDecoration(
            hintText: '',
            filled: true,
            fillColor: const Color(0xFFB0C4DE),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide.none,
            ),
          ),
          validator: (value) {
            if (label.contains('טלפון')) {
              if (value == null || value.isEmpty) {
                return 'מספר הטלפון נדרש';
              } else if (!RegExp(r'^[0-9]{10}$').hasMatch(value)) {
                return 'מספר הטלפון חייב להכיל 10 ספרות';
              }
            }
            return null;
          },
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
        TextFormField(
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
              value: !_obscurePassword,
              onChanged: (bool? value) {
                setState(() {
                  _obscurePassword = !(value ?? false);
                });
              },
            ),
          ),
          validator: (value) {
            if (value == null || value.isEmpty) {
              return null; // Allow empty (means keep old password)
            } else if (value.length < 8) {
              return 'הסיסמה חייבת להכיל לפחות 8 תווים';
            } else if (!RegExp(r'^[a-zA-Z0-9]+$').hasMatch(value)) {
              return 'הסיסמה חייבת להיות באנגלית בלבד (אותיות ומספרים)';
            } else if (!RegExp(r'[a-zA-Z]').hasMatch(value)) {
              return 'הסיסמה חייבת לכלול לפחות אות אחת באנגלית';
            } else if (!RegExp(r'[0-9]').hasMatch(value)) {
              return 'הסיסמה חייבת לכלול לפחות ספרה אחת';
            }
            return null;
          },
        ),
      ],
    );
  }
}
