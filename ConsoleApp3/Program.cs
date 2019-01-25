using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace ConsoleApp3
{
    // Пример Оракл

    class Program
    {
        //http://appsjack.blogspot.com/2010/09/pass-custom-udt-types-to-oracle-stored.html
        static void Main(string[] args)
        {
            using (var con = new OracleConnection("Data Source=testAsc; User ID=p1636; Password=p1636"))
            {
                con.Open();
                using (var cmd = new OracleCommand())
                {
                    cmd.Connection = con;
                    cmd.CommandText = "TEST";
                    cmd.CommandType = CommandType.StoredProcedure;
                    var v = new PersonList 
                    {
                        PersonArray = new[]
                        {
                            new Person
                            {
                                Address = "Kolkata",
                                Age = 20,
                                Name = "Mr.Jhon"
                            },
                            new Person
                            {
                                Address = "Kolkata2",
                                Age = 21,
                                Name = "Mr.Jhon2"
                            }
                        }
                    };
                    var p = CreateCustomTypeArrayInputParameter("persons_arr", Person.ARR_OBJ_TYPE_NAME, v);
                    cmd.Parameters.Add(p);

                    var p2 = CreateCursorParameter("resultCursor");
                    cmd.Parameters.Add(p2);



                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["AGE"].ToString());
                        }
                    }
                }
                con.Close();
            }

            Console.ReadKey();

            //OracleCommand objCmd = new OracleCommand(strSql, objCon);
            //objCmd.CommandType = CommandType.Text;

        }

        public static OracleParameter CreateCursorParameter(string name)
        {
            OracleParameter prm = new OracleParameter(name, OracleDbType.RefCursor);
            prm.Direction = ParameterDirection.Output;
            return prm;
        }

        public static OracleParameter CreateCustomTypeArrayInputParameter<T>(string name, string oracleUDTName, T value) where T : INullable
        {
            OracleParameter parameter = new OracleParameter();
            parameter.ParameterName = name;
            parameter.OracleDbType = OracleDbType.Array;
            parameter.Direction = ParameterDirection.Input;
            parameter.UdtTypeName = oracleUDTName;
            parameter.Value = value;
            return parameter;
        }
    }

    [OracleCustomTypeMapping(Person.ARR_OBJ_TYPE_NAME)]
    public class PersonListFactory : IOracleCustomTypeFactory, IOracleArrayTypeFactory
    {
        #region IOracleCustomTypeFactory Members
        public IOracleCustomType CreateObject()
        {
            return new PersonList();
        }

        #endregion

        #region IOracleArrayTypeFactory Members
        public Array CreateArray(int numElems)
        {
            return new Person[numElems];
        }

        public Array CreateStatusArray(int numElems)
        {
            return null;
        }

        #endregion
    }

    [OracleCustomTypeMapping(Person.OBJ_TYPE_NAME)]
    public class PersonBOFactory : IOracleCustomTypeFactory
    {
        public virtual IOracleCustomType CreateObject() => new Person();
    }

    public class Person : IOracleCustomType, INullable
    {
        const string FIELD_ADDRESS = "ADDRESS";
        const string FIELD_AGE = "AGE";
        const string FIELD_NAME = "NAME";

        public const string OBJ_TYPE_NAME = "ODP_PERSON_TYPE";
        public const string ARR_OBJ_TYPE_NAME = "PERSON_ARRAY_TYPE";

        [OracleObjectMapping(FIELD_ADDRESS)]
        public string Address { get; set; }

        [OracleObjectMapping(FIELD_AGE)]
        public int Age { get; set; }

        [OracleObjectMapping(FIELD_NAME)]
        public string Name { get; set; }

        public void FromCustomObject(OracleConnection con, IntPtr pUdt)
        {
            OracleUdt.SetValue(con, pUdt, FIELD_NAME, this.Name);
            OracleUdt.SetValue(con, pUdt, FIELD_ADDRESS, this.Address);
            OracleUdt.SetValue(con, pUdt, FIELD_AGE, this.Age);
        }

        public void ToCustomObject(OracleConnection con, IntPtr pUdt)
        {
            this.Name = (string)OracleUdt.GetValue(con, pUdt, FIELD_NAME);
            this.Address = (string)OracleUdt.GetValue(con, pUdt, FIELD_ADDRESS);
            this.Age = (int)OracleUdt.GetValue(con, pUdt, FIELD_AGE);
        }

        public static Person Null
        {
            get
            {
                Person company = new Person();
                company.objectIsNull = true;
                return company;
            }
        }

        private bool objectIsNull;
        public bool IsNull => objectIsNull;
    }

    public class PersonList : INullable, IOracleCustomType
    {
        [OracleArrayMapping]
        public Person[] PersonArray;

        private bool objectIsNull;

        #region INullable Members

        public bool IsNull => objectIsNull;

        public static PersonList Null
        {
            get
            {
                PersonList obj = new PersonList();
                obj.objectIsNull = true;
                return obj;
            }
        }

        #endregion

        #region IOracleCustomType Members

        public void FromCustomObject(OracleConnection con, IntPtr pUdt)
        {
            OracleUdt.SetValue(con, pUdt, 0, PersonArray);
        }

        public void ToCustomObject(OracleConnection con, IntPtr pUdt)
        {
            PersonArray = (Person[])OracleUdt.GetValue(con, pUdt, 0);
        }

        #endregion
    }



}
