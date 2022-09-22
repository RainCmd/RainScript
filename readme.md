## 雨言
**该语言主要目的是为了在能实现功能的基础上尽量精简使用符号数量，**
**从而方便在手机平板等设备上随时随地编程而不再受限于键盘**
支持面向对象的并且具有垃圾回收的静态类型语言，支持编译为基于定点数运算。
脚本内所有函数都是协程，可以在任意位置使用wait关键字等下次update再继续执行。
对源码编译后会生成执行需要的库文件、编译时引用的引用表和调试用的符号表。
虚拟机只需要有库文件就可以执行，编译器引用其他库时只需要引用表就可以编译成功，
代码发生异常退出时，可以获取当前栈数据，通过栈数据在符号表便可以查到当前执行的调用栈。
程序集没有默认执行函数，所以运行虚拟机后需要查找函数手动调用。
``` csharp
    var handle = vm.GetFunctionHandle("入口函数名", "程序集名");
    var invoker = vm.Invoker(handle);
    invoker.Start(true, false);
```

## 关键字

| 关键字 | 解释 | 关键字 | 解释 |
| :---: | :---: | :---: | :---: |
| namespace | 命名空间 | import | 导入命名空间 |
| native | 用于和宿主语言交互的函数 | public | 任意位置都可访问 |
| internal | 当前程序集中任意位置可访问 | space | 仅当前命名空间内可访问 |
| protected | 当前类和子类可访问 | private | 仅当前类可访问 |
| ~~struct~~ | 保留关键字 | class | 类 |
| interface | 接口 | ~~new~~ | 保留关键字 |
| const | 常量 | kernel | 一些基本类型和函数所在的命名空间 |
| global | 程序集命名空间之上的命名空间 | base | 用来访问父对象的成员 |
| this | 当前对象 | true | TRUE |
| false | FALSE | null | 表示handle类型或entity类型的空值 |
| var | 用来隐式申明局部变量 | bool | 布尔类型 |
| int | 整数类型（64位） | real | 实数类型（双精度浮点数/48位整数16位小数的定点数） |
| real2 | 实数的二维向量 | real3 | 实数的三维向量 |
| real4 | 实数的四维向量 | string | 字符串类型（只有长度为0，不可为null） |
| handle | 句柄类型 class类型/函数/协程/数组 的基类 | entity | 用来表示宿主语言中的对象 |
| function | 函数（委托）类型 | coroutine | 协程类型 |
| array | 所有数组类型的基类 | if | 条件判断 |
| elif | 跟在if/loop后的条件判断 | else | if/elif/loop判断为false时执行的分支 |
| loop | 循环 | ~~foreach~~ | 保留关键字 |
| break | 跳出循环 | continue | 跳过循环中后续语句 |
| return | 函数返回 | is | 尝试类型转换，如果转换成功则返回true，否则返回false |
| as | 尝试类型转换，如果转换成功则返回对象，否则返回null | start | 开启新的协程 |
| wait | 等待 | exit | 终止协程，终止码必须非0 |
| ~~try~~ | 保留关键字 | ~~catch~~ | 保留关键字 |
| ~~finally~~ | 保留关键字 |   |   |

## 数据类型

* `bool`:布尔类型
* `int`:整数类型（64位）
* `real`:实数类型（双精度浮点数/48位整数16位小数的定点数）
* `real2`:实数的二维向量
* `real3`:实数的三维向量
* `real4`:实数的四维向量
* `string`:字符串类型（只有长度为0，不可为null）
* `handle`:句柄类型 class类型/函数/协程/数组 的基类，存储在托管堆中，生命周期受垃圾回收机制的影响
* `entity`:实体类型 用来表示宿主语言中的对象，当虚拟机中没有该类型的引用时，对应的宿主语言中的对象才会被释放
* `function`:函数指针（委托）类型，可以指向任意参数和返回值类型一致的函数，将函数赋值给函数指针时会创建function对象，指针间的赋值则不会创建新对象
* `coroutine`:协程类型，*start*关键字跟函数调用可以创建协程对象，调用的函数返回值必须与协程的返回值类型一致


## 程序结构

根据文本的缩进来确定脚本的层次，文件的第一个有效行或命名的缩进就是当前文件命名空间全局缩进，
后续任何内容的缩进都不可小于该缩进。与全局缩进对齐的都是申明语句。

## 基本语法

# namespace 命名空间
文件的命名空间都是当前程序集名，以当前命名空间的缩进输入namespace关键字加空间名便可以开启新的命名空间，
后续第一个有效且缩进大于当前空间声明行缩进的行为新空间的起始行，改行的缩进为新空间的缩进。
``` js
namespace 空间名1
    空间1的代码
    namespace 空间名2
        空间2的代码
    空间1的代码
```
# import 引用命名空间
可以在空间缩进的任意位置使用import来引用其他命名空间
``` js
    import 空间名
```
# 注释
双斜杠后的内容为注释，目前只支持单行注释。
``` js
    //这是注释
```

# 各种申明
申明只能在命名空间的缩进中，函数声明时参数名可以省略，如果函数没有返回值，则函数名前不需要任何字符
``` rs
native NativeFunc()//声明一个本地函数 NativeFunc
GlobalFunc()//声明一个全局函数 GlobalFunc
interface ITest//声明一个接口 ITest
class TestClass//声明一个类 TestClass
function Func()//声明一个函数指针（委托）类型 Func
coroutine Coro//声明一个协程 Coro
```

# 元组
表示返回值数量不为1的表达式，可以通过[]来取值，也可以组合成新的元组，
元组的索引必须为常量，索引范围在0到元素数量之间，为负数的话会先加上元素数量后再计算
元组可以当做一组参数直接传给函数。
函数也可以返回多个返回值，这类函数调用后的表达式也是一个元组。
``` js
int,real,string Func()
    return 1,2.3,"abc"

Func2(int,real,string)
Func3(real,string,int,string)

Entry()
    Func2(Func())                   //函数调用的表达式是个元组，可以直接作为其他函数参数
    Func3(Func()[1,2,0,2])          //元组重组为新的元组并作为函数参数
    Func3(.1,Func()[-1,0],"ABC")    //重组的元组与参数一起作为函数参数，-1在编译时会加上元素数量3，最终被当作2来解析
```

# lambda表达式
无参的表达式在=>无需任何符号，如果大于一个参数或返回值，则需要括号
支持闭包，但如果创建闭包会额外创建对象，所以编译时会根据表达式中是否引用到当前上下文的变量来自动判断是否需要创建闭包
```rs
function int Func(int)
function int,string Func2()

class A
    public int i
    public MFunc()
        Func f=v=>v+1               //不会创建闭包
        f=v=>v+i                    //变量i是成员变量，所以会创建闭包
        Func2 f2= =>(v(123),"abc")  //变量v是局部变量，所以会创建闭包
```

# 协程
通过*start*关键字可以开启协程执行方法，注意：`协程不能直接执行native方法`
```rs
Func(string n,int i)
    loop i-->0
        wait
        Print(n+":"+i.ToString())
Entry()
    start Func("协程A",4)
    start Func("协程B",6)
```
协程执行完成后可以像元组一样取得协程的返回值，方括号内也可以不填任何内容
```rs
coroutine<int,string> Coro
int,string Func()
    wait 10
    return 123,"abc"
Entry()
    Coro c=start Func()
    loop c.GetState()!=2            //协程状态码 0:未开始执行；1:执行中;2:执行完成;3:被主动取消;4:已失效
        wait
    var a,var b=c[]                 //a的值是 123；b的值是 "abc"
```

# 常量的声明
整数和实数常量中间可以用'_'分隔，但引号加字符也可以表示整数，反斜杠可以转义在单引号中同样适用
```js
const int a=1234_5678
const real b=.1234_5678
const int c='abc'                   //最终c的值为 0x616263
const int d='\x12a'                 //最终d的值为 0x1261
const int e=0x1234
const int f=0b0111_0010_0110_0001_0110_1001_0110_1110
const string s="\u4e00\x1b[33mhello word\x1b[0m"
```

# 向量的计算
支持real2/real3/real4向量的基本运算。
``` js
var r4=real4(1,2,3,4)
var a=r4.xyz+r4.yzw             //变量a会被解析为real3类型，计算结果为(3,5,7)
var b=r4.xyz*r4.yzw             //变量b的计算结果为(2,6,12)
var c=r4.xyz/r4.yzw             //变量c的计算结果为(.5,.666666,.75)
var d=2/r4                      //变量d的计算结果为(2,1,.666666,.5)
```

# 字符串和数组
字符串和数组的索引下标都可以为负数，为负数时会先加一次长度再计算
当索引数量为2时都是裁剪当前串
```js
var a="hello word"
var b=a[1]                          //变量b结果为'e'的ascii码
var c=a[2,-3]                       //变量c结果为字符串 "llo wo"
```

# 继承
接口只能继承多个接口，类可以继承一个类和多个接口，如果继承类的话，父类必须在继承列表的第一个
```rs
interface ITestA
interface ITestB ITestA
class A
class B A ITestB ITestA
```

# 实例化
类型名后加()便可以实例化对象，括号内填构造函数的参数，数组则类型名加方括号即可，方括号内填数组长度，
也可以用花括号来初始化并赋值数组
```rs
class A
    public A()
    public A(int,string)

Entry()
    var a=A()
    a=A(1,"2")
    var b=A[10]                     //长度为10的A数组
    var c=A[10][]                   //长度为10的A数组的数组
    var d=int{1,2,3}                //长度为3，内容为1,2,3的整数数组
    int[]e={3,2,1}                  //长度为3，内容为3,2,1的整数数组
```

# 类型转换
类型名后加&表示类型转换，也可以通过is和as关键字来进行类型转换。
real和int之间、向量之间的类型转换会自动进行，无需显式指定
```rs
class A
class B A

Entry()
    A a=B()
    B b=B&a
    bool c=a is B bb                //变量名bb非必须
    B d=a as B                      //变量d不为null
    a=A()
    B e=a as B                      //变量e为null 
```

# 函数的override
所有成员函数都是虚函数，如果子类中成员函数名和参数类型与父类一致时就会override，如果override函数返回值类型不一致会编译异常
```rs
interface ITest
    IFunc()
class A
    public IFunc()
        Print("IFunc")
    public MFunc()
        Print("MFunc in A")
class B A ITest
    public MFunc()
        Print("MFunc in B")

Entry()
    ITest i=B()
    i.IFunc()                       //输出 "IFunc"
    (B&i).Mfunc()                   //输出 "MFunc in B"
```

# 运算符
支持的基础运算符有:
&,&&,&=,|,||,|=,^,^=,<,<=,<<,<<=,>,>=,>>,>>=,+,++,+=,-,--,-=,*,*=,/,/=,%,%=,!,!=,`<br>
另外还支持问号条件运算：
```js
var a=true
var b=a?1:2
a?Func1():Func2()
```
问号点运算：
```js
var a=A()
a?.Func()
```
问号括号运算：
```rs
function Func

Entry()
    Func f=null
    f?()                            //不会报空引用错
```

